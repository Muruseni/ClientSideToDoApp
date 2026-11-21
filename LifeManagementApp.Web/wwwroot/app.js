const state = { tasks: [], categories: [], editing: null, categoryId: '', sort: 'priority' };
const $ = id => document.getElementById(id);
const today = () => new Date().toISOString().slice(0, 10);
const dateText = value => new Date(value).toLocaleDateString(undefined, { dateStyle: 'medium' });

// Keep fetch error handling in one place so every form displays API validation consistently.
async function api(url, options) {
  const response = await fetch(url, { headers: { 'Content-Type': 'application/json' }, ...options });
  if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.message || Object.values(body.errors || {}).flat().join(' ') || 'The request failed.'); }
  return response.status === 204 ? null : response.json();
}

async function load() {
  // Load both resources together so the UI never renders a category selector
  // against a task list from a different database snapshot.
  try { [state.tasks, state.categories] = await Promise.all([api('/api/tasks'), api('/api/categories')]); render(); }
  catch (error) { $('task-message').textContent = error.message; }
}

function taskMarkup(task) {
  return `<div class="task ${task.isDone ? 'done' : ''}"><button class="button secondary" data-action="toggle" data-id="${task.id}">${task.isDone ? 'Undo' : 'Done'}</button><div class="task-main"><div class="task-title">${escapeHtml(task.title)}</div><div class="task-meta">Due ${dateText(task.dueDate)} · Priority ${task.priority}/5${task.categoryName ? ` · ${escapeHtml(task.categoryName)}` : ''}</div></div><div class="task-actions"><button data-action="edit" data-id="${task.id}">Edit</button><button class="danger" data-action="delete" data-id="${task.id}">Delete</button></div></div>`;
}

function render() {
  // Filtering happens before sorting so the selected sort order applies only to visible tasks.
  const filtered = state.tasks.filter(task => !state.categoryId || String(task.categoryId) === state.categoryId);
  const sorted = [...filtered].sort((a, b) => state.sort === 'due' ? new Date(a.dueDate) - new Date(b.dueDate) : state.sort === 'title' ? a.title.localeCompare(b.title) : b.priority - a.priority || new Date(a.dueDate) - new Date(b.dueDate));
  const open = sorted.filter(task => !task.isDone);
  $('all-tasks').innerHTML = sorted.length ? sorted.map(taskMarkup).join('') : '<p>No tasks yet. Add one to get started.</p>';
  $('next-tasks').innerHTML = open.length ? open.slice(0, 3).map(taskMarkup).join('') : '<p>No open tasks yet. Add one from the To-dos tab.</p>';
  $('home-summary').textContent = open.length ? `${open.length} open task${open.length === 1 ? '' : 's'} waiting for you.` : 'You are all caught up.';
  $('task-category').innerHTML = '<option value="">No category</option>' + state.categories.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join('');
  $('filter-category').innerHTML = '<option value="">All categories</option>' + state.categories.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join('');
  $('filter-category').value = state.categoryId; $('sort-tasks').value = state.sort;
}

function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, character => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', "'":'&#39;', '"':'&quot;' }[character])); }
function showView(view) { document.querySelectorAll('.view').forEach(element => element.classList.toggle('hidden', element.id !== `${view}-view`)); document.querySelectorAll('.nav-button').forEach(button => button.classList.toggle('active', button.dataset.view === view)); }
function openDialog(task = null) { state.editing = task; $('dialog-title').textContent = task ? 'Edit a to-do' : 'Add a to-do'; $('task-id').value = task?.id || ''; $('task-title').value = task?.title || ''; $('task-priority').value = task?.priority || 3; $('priority-output').value = $('task-priority').value; $('task-category').value = task?.categoryId || ''; $('task-due').value = task ? task.dueDate.slice(0, 10) : today(); $('form-message').textContent = ''; $('task-dialog').showModal(); }

async function saveTask(event) { event.preventDefault(); $('form-message').textContent = ''; const task = { title: $('task-title').value, priority: Number($('task-priority').value), dueDate: $('task-due').value, categoryId: $('task-category').value ? Number($('task-category').value) : null, isDone: state.editing?.isDone || false }; try { const id = $('task-id').value; await api(id ? `/api/tasks/${id}` : '/api/tasks', { method: id ? 'PUT' : 'POST', body: JSON.stringify(task) }); $('task-dialog').close(); await load(); } catch (error) { $('form-message').textContent = error.message; } }

// The same PUT path handles completion toggles and edits, keeping persistence behavior identical.
async function handleTaskAction(event) { const button = event.target.closest('[data-action]'); if (!button) return; const task = state.tasks.find(item => item.id === Number(button.dataset.id)); if (!task) return; try { if (button.dataset.action === 'edit') openDialog(task); if (button.dataset.action === 'toggle') await api(`/api/tasks/${task.id}`, { method:'PUT', body: JSON.stringify({ ...task, dueDate: task.dueDate, isDone: !task.isDone }) }); if (button.dataset.action === 'delete' && confirm(`Delete “${task.title}”?`)) await api(`/api/tasks/${task.id}`, { method:'DELETE' }); await load(); } catch (error) { $('task-message').textContent = error.message; } }
async function loadWeather() {
  const city = $('city').value.trim();
  $('weather-result').textContent = 'Loading weather…';
  try {
    let weather;
    try {
      // Prefer the local endpoint so the web host remains the single API contract.
      weather = await api(`/api/weather?city=${encodeURIComponent(city)}`);
    } catch {
      // A blocked local .NET host should not make the public weather lookup unusable.
      weather = await loadWeatherDirectly(city);
    }
    const alert = weather.kitesurfConditions ? 'Kitesurf alert: conditions are available.' : 'Kitesurf alert: wind is below 5 m/s.';
    $('weather-result').innerHTML = `<div class="weather-temp">${weather.temperature.toFixed(0)}°C</div><div>${escapeHtml(weather.description)} in ${escapeHtml(weather.city)}</div><small>Feels like ${weather.feelsLike.toFixed(0)}°C · Wind ${weather.windSpeed.toFixed(1)} m/s</small><div>${alert}</div>`;
  } catch (error) {
    $('weather-result').textContent = error.message || 'Weather is unavailable right now.';
  }
}

async function loadWeatherDirectly(city) {
  if (!city) throw new Error('Enter a city.');
  const locationResponse = await fetch(`https://geocoding-api.open-meteo.com/v1/search?name=${encodeURIComponent(city)}&count=1&language=en&format=json`);
  if (!locationResponse.ok) throw new Error('Weather lookup failed.');
  const location = (await locationResponse.json()).results?.[0];
  if (!location) throw new Error('Location was not found.');
  const forecastResponse = await fetch(`https://api.open-meteo.com/v1/forecast?latitude=${location.latitude}&longitude=${location.longitude}&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m&wind_speed_unit=ms&timezone=auto`);
  if (!forecastResponse.ok) throw new Error('Weather data was unavailable.');
  const current = (await forecastResponse.json()).current;
  if (!current) throw new Error('Weather data was unavailable.');
  return { city: location.name || city, temperature: current.temperature_2m, feelsLike: current.apparent_temperature, windSpeed: current.wind_speed_10m, kitesurfConditions: current.wind_speed_10m >= 5, description: describeWeatherCode(current.weather_code) };
}

function describeWeatherCode(code) { return ({ 0:'Clear sky', 1:'Mostly clear', 2:'Mostly clear', 3:'Overcast', 45:'Foggy', 48:'Foggy', 51:'Drizzle', 53:'Drizzle', 55:'Drizzle', 61:'Rain', 63:'Rain', 65:'Rain', 71:'Snow', 73:'Snow', 75:'Snow', 80:'Rain showers', 81:'Rain showers', 82:'Rain showers', 95:'Thunderstorm', 96:'Thunderstorm', 99:'Thunderstorm' })[code] || 'Unknown conditions'; }

async function addCategory() { const name = $('new-category').value.trim(); if (!name) return; try { await api('/api/categories', { method:'POST', body: JSON.stringify({ name }) }); $('new-category').value = ''; await load(); } catch (error) { $('task-message').textContent = error.message; } }

document.querySelectorAll('.nav-button').forEach(button => button.addEventListener('click', () => showView(button.dataset.view)));
['open-tasks'].forEach(id => $(id).addEventListener('click', () => showView('tasks')));
['add-task','home-add'].forEach(id => $(id).addEventListener('click', () => openDialog()));
document.querySelectorAll('.task-list').forEach(list => list.addEventListener('click', handleTaskAction));
$('task-form').addEventListener('submit', saveTask); $('task-priority').addEventListener('input', () => $('priority-output').value = $('task-priority').value); $('weather-load').addEventListener('click', loadWeather); $('weather-refresh').addEventListener('click', loadWeather);
$('filter-category').addEventListener('change', event => { state.categoryId = event.target.value; render(); }); $('sort-tasks').addEventListener('change', event => { state.sort = event.target.value; render(); }); $('add-category').addEventListener('click', addCategory);
load();
