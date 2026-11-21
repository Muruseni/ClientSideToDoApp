const state = { tasks: [], categories: [], editing: null };
const $ = id => document.getElementById(id);
const today = () => new Date().toISOString().slice(0, 10);
const dateText = value => new Date(value).toLocaleDateString(undefined, { dateStyle: 'medium' });

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
  const open = state.tasks.filter(task => !task.isDone).sort((a,b) => b.priority-a.priority || new Date(a.dueDate)-new Date(b.dueDate));
  $('all-tasks').innerHTML = state.tasks.length ? state.tasks.map(taskMarkup).join('') : '<p>No tasks yet. Add one to get started.</p>';
  $('next-tasks').innerHTML = open.length ? open.slice(0, 3).map(taskMarkup).join('') : '<p>No open tasks yet. Add one from the To-dos tab.</p>';
  $('home-summary').textContent = open.length ? `${open.length} open task${open.length === 1 ? '' : 's'} waiting for you.` : 'You are all caught up.';
  $('task-category').innerHTML = '<option value="">No category</option>' + state.categories.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join('');
}

function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, character => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', "'":'&#39;', '"':'&quot;' }[character])); }
function showView(view) { document.querySelectorAll('.view').forEach(element => element.classList.toggle('hidden', element.id !== `${view}-view`)); document.querySelectorAll('.nav-button').forEach(button => button.classList.toggle('active', button.dataset.view === view)); }
function openDialog(task = null) { state.editing = task; $('dialog-title').textContent = task ? 'Edit a to-do' : 'Add a to-do'; $('task-id').value = task?.id || ''; $('task-title').value = task?.title || ''; $('task-priority').value = task?.priority || 3; $('priority-output').value = $('task-priority').value; $('task-category').value = task?.categoryId || ''; $('task-due').value = task ? task.dueDate.slice(0, 10) : today(); $('form-message').textContent = ''; $('task-dialog').showModal(); }

async function saveTask(event) { event.preventDefault(); $('form-message').textContent = ''; const task = { title: $('task-title').value, priority: Number($('task-priority').value), dueDate: $('task-due').value, categoryId: $('task-category').value ? Number($('task-category').value) : null, isDone: state.editing?.isDone || false }; try { const id = $('task-id').value; await api(id ? `/api/tasks/${id}` : '/api/tasks', { method: id ? 'PUT' : 'POST', body: JSON.stringify(task) }); $('task-dialog').close(); await load(); } catch (error) { $('form-message').textContent = error.message; } }

async function handleTaskAction(event) { const button = event.target.closest('[data-action]'); if (!button) return; const task = state.tasks.find(item => item.id === Number(button.dataset.id)); if (!task) return; try { if (button.dataset.action === 'edit') openDialog(task); if (button.dataset.action === 'toggle') await api(`/api/tasks/${task.id}`, { method:'PUT', body: JSON.stringify({ ...task, dueDate: task.dueDate, isDone: !task.isDone }) }); if (button.dataset.action === 'delete' && confirm(`Delete “${task.title}”?`)) await api(`/api/tasks/${task.id}`, { method:'DELETE' }); await load(); } catch (error) { $('task-message').textContent = error.message; } }
async function loadWeather() { const city = $('city').value.trim(); $('weather-result').textContent = 'Loading weather…'; try { const weather = await api(`/api/weather?city=${encodeURIComponent(city)}`); $('weather-result').innerHTML = `<div class="weather-temp">${weather.temperature.toFixed(0)}°C</div><div>${escapeHtml(weather.description)} in ${escapeHtml(weather.city)}</div><small>Feels like ${weather.feelsLike.toFixed(0)}°C · Wind ${weather.windSpeed.toFixed(0)} km/h</small>`; } catch (error) { $('weather-result').textContent = error.message; } }

document.querySelectorAll('.nav-button').forEach(button => button.addEventListener('click', () => showView(button.dataset.view)));
['open-tasks'].forEach(id => $(id).addEventListener('click', () => showView('tasks')));
['add-task','home-add'].forEach(id => $(id).addEventListener('click', () => openDialog()));
document.querySelectorAll('.task-list').forEach(list => list.addEventListener('click', handleTaskAction));
$('task-form').addEventListener('submit', saveTask); $('task-priority').addEventListener('input', () => $('priority-output').value = $('task-priority').value); $('weather-load').addEventListener('click', loadWeather); $('weather-refresh').addEventListener('click', loadWeather);
load();
