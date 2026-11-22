// The browser keeps one small state object; render() turns this state into HTML.
// It lets the same UI work with the ASP.NET API or with the offline SQLite fallback.
const state = { tasks: [], categories: [], editing: null, categoryId: '', sort: 'priority', showArchived: false, hideDone: false };
const $ = id => document.getElementById(id);
const today = () => new Date().toISOString().slice(0, 10);
const dateText = value => new Date(value).toLocaleDateString(undefined, { dateStyle: 'medium' });
const browserDatabaseName = 'life-management-browser';
const browserStoreName = 'state';
const browserSqlJsVersion = '1.13.0';
let browserSqlite;
let browserStorage;

const supportiveMessages = [
  'Keep your important things moving.',
  'Small steps still move you forward.',
  'You can make today count.',
  'One task at a time is enough.',
  'You are doing great work.',
  'Make space for what matters most.',
  'Progress is progress, however small.'
];

// Remember the previous choice so a normal browser reload selects a different
// supportive message whenever at least two choices are available.
function chooseSupportiveMessage() {
  const previous = sessionStorage.getItem('supportive-message');
  const choices = supportiveMessages.filter(message => message !== previous);
  const message = choices[Math.floor(Math.random() * choices.length)];
  sessionStorage.setItem('supportive-message', message);
  return message;
}

const supportiveMessage = chooseSupportiveMessage();

// Keep fetch error handling in one place so every form displays API validation consistently.
async function api(url, options) {
  const response = await fetch(url, { headers: { 'Content-Type': 'application/json' }, ...options });
  if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.message || Object.values(body.errors || {}).flat().join(' ') || 'The request failed.'); }
  return response.status === 204 ? null : response.json();
}

// Keep SQLite in the browser so the UI remains usable when the local ASP.NET
// host is unavailable or blocked by Windows application control. IndexedDB is
// only the durable file container; sql.js performs the actual SQLite queries.
function openBrowserStorage() {
  if (browserStorage) return Promise.resolve(browserStorage);
  return new Promise((resolve, reject) => {
    if (!('indexedDB' in window)) { resolve(null); return; }
    const request = indexedDB.open(browserDatabaseName, 1);
    request.onupgradeneeded = () => request.result.createObjectStore(browserStoreName);
    request.onsuccess = () => { browserStorage = request.result; resolve(browserStorage); };
    request.onerror = () => reject(request.error);
  });
}

function readBrowserRecord(database) {
  return new Promise((resolve, reject) => {
    const request = database.transaction(browserStoreName, 'readonly').objectStore(browserStoreName).get('snapshot');
    request.onsuccess = () => resolve(request.result || null);
    request.onerror = () => reject(request.error);
  });
}

function writeBrowserRecord(database, data) {
  return new Promise((resolve, reject) => {
    const request = database.transaction(browserStoreName, 'readwrite').objectStore(browserStoreName).put(data, 'snapshot');
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error);
  });
}

function browserRows(database, query) {
  const result = database.exec(query);
  return result.length ? result[0].values : [];
}

function browserSchema(database) {
  database.run(`
    PRAGMA foreign_keys = ON;
    CREATE TABLE IF NOT EXISTS categories (id INTEGER PRIMARY KEY, name TEXT NOT NULL UNIQUE);
    CREATE TABLE IF NOT EXISTS tasks (
      id INTEGER PRIMARY KEY,
      title TEXT NOT NULL,
      priority INTEGER NOT NULL,
      dueDate TEXT NOT NULL,
      isDone INTEGER NOT NULL,
      archived INTEGER NOT NULL DEFAULT 0,
      categoryId INTEGER NULL,
    FOREIGN KEY (categoryId) REFERENCES categories(id) ON DELETE SET NULL
    );`);
  const columns = browserRows(database, "PRAGMA table_info(tasks)");
  if (!columns.some(column => column[1] === 'archived')) database.run('ALTER TABLE tasks ADD COLUMN archived INTEGER NOT NULL DEFAULT 0');
}

async function getBrowserSqlite() {
  if (browserSqlite) return browserSqlite;
  if (typeof window.initSqlJs !== 'function') throw new Error('SQLite could not be loaded in this browser. Check your network connection and reload.');
  const storage = await openBrowserStorage();
  if (!storage) throw new Error('This browser does not support offline SQLite storage.');
  const SQL = await window.initSqlJs({ locateFile: file => `https://cdn.jsdelivr.net/npm/sql.js@${browserSqlJsVersion}/dist/${file}` });
  const stored = await readBrowserRecord(storage);
  browserSqlite = stored?.sqlite ? new SQL.Database(new Uint8Array(stored.sqlite)) : new SQL.Database();
  browserSchema(browserSqlite);

  // Migrate the JSON snapshot created by the previous offline implementation once.
  if (!stored?.sqlite && stored?.categories?.length) {
    browserSqlite.run('BEGIN');
    try {
      for (const category of stored.categories) browserSqlite.run('INSERT OR IGNORE INTO categories (id, name) VALUES (?, ?)', [category.id, category.name]);
      for (const task of stored.tasks || []) browserSqlite.run('INSERT OR REPLACE INTO tasks (id, title, priority, dueDate, isDone, archived, categoryId) VALUES (?, ?, ?, ?, ?, ?, ?)', [task.id, task.title, task.priority, task.dueDate, task.isDone ? 1 : 0, task.archived ? 1 : 0, task.categoryId || null]);
      browserSqlite.run('COMMIT');
    } catch (error) {
      browserSqlite.run('ROLLBACK');
      throw error;
    }
  }
  await writeBrowserRecord(storage, { sqlite: Array.from(browserSqlite.export()) });
  return browserSqlite;
}

// Read a complete browser snapshot so task and category selectors always agree.
async function localData() {
  const database = await getBrowserSqlite();
  const categories = browserRows(database, 'SELECT id, name FROM categories ORDER BY name').map(([id, name]) => ({ id, name }));
  const categoryNames = new Map(categories.map(category => [category.id, category.name]));
  const tasks = browserRows(database, 'SELECT id, title, priority, dueDate, isDone, archived, categoryId FROM tasks ORDER BY id').map(([id, title, priority, dueDate, isDone, archived, categoryId]) => ({ id, title, priority, dueDate, isDone: Boolean(isDone), archived: Boolean(archived), categoryId, categoryName: categoryNames.get(categoryId) || '' }));
  return { tasks, categories };
}

async function saveLocalData(tasks, categories) {
  const database = await getBrowserSqlite();
  database.run('BEGIN');
  try {
    database.run('DELETE FROM tasks');
    database.run('DELETE FROM categories');
    for (const category of categories) database.run('INSERT INTO categories (id, name) VALUES (?, ?)', [category.id, category.name]);
    for (const task of tasks) database.run('INSERT INTO tasks (id, title, priority, dueDate, isDone, archived, categoryId) VALUES (?, ?, ?, ?, ?, ?, ?)', [task.id, task.title, task.priority, task.dueDate, task.isDone ? 1 : 0, task.archived ? 1 : 0, task.categoryId || null]);
    database.run('COMMIT');
  } catch (error) {
    database.run('ROLLBACK');
    throw error;
  }
  await writeBrowserRecord(browserStorage, { sqlite: Array.from(database.export()) });
}

function nextLocalId(items) { return items.reduce((highest, item) => Math.max(highest, Number(item.id) || 0), 0) + 1; }

async function load() {
  // Load both resources together so the UI never renders a category selector
  // against a task list from a different database snapshot.
  try {
    [state.tasks, state.categories] = await Promise.all([api('/api/tasks'), api('/api/categories')]);
    render();
  } catch {
    // Fall back to browser storage instead of leaving the page unusable when
    // the local .NET API cannot start under the device's security policy.
    ({ tasks: state.tasks, categories: state.categories } = await localData());
    render();
  }
}

function taskMarkup(task) {
  const archiveLabel = task.archived ? 'Unarchive' : 'Archive';
  return `<div class="task ${task.isDone ? 'done' : ''}"><button class="button secondary" data-action="toggle" data-id="${task.id}">${task.isDone ? 'Undo' : 'Done'}</button><div class="task-main"><div class="task-title">${escapeHtml(task.title)}</div><div class="task-meta">Due ${dateText(task.dueDate)} · Priority ${task.priority}/5${task.categoryName ? ` · ${escapeHtml(task.categoryName)}` : ''}</div></div><div class="task-actions"><button data-action="edit" data-id="${task.id}">Edit</button><button data-action="archive" data-id="${task.id}">${archiveLabel}</button><button class="danger" data-action="delete" data-id="${task.id}">Delete</button></div></div>`;
}

function render() {
  // Filtering happens before sorting so the selected sort order applies only to visible tasks.
  const filtered = state.tasks.filter(task => task.archived === state.showArchived && (!state.hideDone || state.showArchived || !task.isDone) && (!state.categoryId || String(task.categoryId) === state.categoryId));
  const sorted = [...filtered].sort((a, b) => state.sort === 'due' ? new Date(a.dueDate) - new Date(b.dueDate) : state.sort === 'title' ? a.title.localeCompare(b.title) : b.priority - a.priority || new Date(a.dueDate) - new Date(b.dueDate));
  const open = state.tasks.filter(task => !task.isDone && !task.archived);
  const homeTasks = [...open]
    .sort((a, b) => b.priority - a.priority || new Date(a.dueDate) - new Date(b.dueDate))
    .slice(0, 3);
  $('all-tasks').innerHTML = sorted.length ? sorted.map(taskMarkup).join('') : '<p>No tasks yet. Add one to get started.</p>';
  $('next-tasks').innerHTML = homeTasks.length ? homeTasks.map(taskMarkup).join('') : '<p>No open tasks yet. Add one from the To-dos tab.</p>';
  $('home-summary').textContent = open.length ? `${open.length} open task${open.length === 1 ? '' : 's'} waiting for you.` : 'You are all caught up.';
  $('supportive-message').textContent = supportiveMessage;
  $('supportive-message-todos').textContent = supportiveMessage;
  $('task-category').innerHTML = '<option value="">No category</option>' + state.categories.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join('');
  $('filter-category').innerHTML = '<option value="">All categories</option>' + state.categories.map(c => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join('');
  $('filter-category').value = state.categoryId; $('sort-tasks').value = state.sort;
  $('archive-view').textContent = state.showArchived ? 'Hide Archived Tasks' : 'View Archived Tasks';
  $('hide-done').textContent = state.hideDone ? 'Show Done' : 'Hide Done';
}

function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, character => ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', "'":'&#39;', '"':'&quot;' }[character])); }
function showView(view) { document.querySelectorAll('.view').forEach(element => element.classList.toggle('hidden', element.id !== `${view}-view`)); document.querySelectorAll('.nav-button').forEach(button => button.classList.toggle('active', button.dataset.view === view)); }
function openDialog(task = null) { state.editing = task; $('dialog-title').textContent = task ? 'Edit a to-do' : 'Add a to-do'; $('task-id').value = task?.id || ''; $('task-title').value = task?.title || ''; $('task-priority').value = task?.priority || 3; $('priority-output').value = $('task-priority').value; $('task-category').value = task?.categoryId || ''; $('task-due').value = task ? task.dueDate.slice(0, 10) : today(); $('form-message').textContent = ''; $('task-dialog').showModal(); }

async function saveTask(event) { event.preventDefault(); $('form-message').textContent = ''; const task = { title: $('task-title').value, priority: Number($('task-priority').value), dueDate: $('task-due').value, categoryId: $('task-category').value ? Number($('task-category').value) : null, isDone: state.editing?.isDone || false, archived: state.editing?.archived || false }; try { const id = $('task-id').value; await api(id ? `/api/tasks/${id}` : '/api/tasks', { method: id ? 'PUT' : 'POST', body: JSON.stringify(task) }); $('task-dialog').close(); await load(); } catch (error) { $('form-message').textContent = error.message; } }

// The same PUT path handles completion toggles and edits, keeping persistence behavior identical.
async function handleTaskAction(event) { const button = event.target.closest('[data-action]'); if (!button) return; const task = state.tasks.find(item => item.id === Number(button.dataset.id)); if (!task) return; try { if (button.dataset.action === 'edit') openDialog(task); if (button.dataset.action === 'toggle') await api(`/api/tasks/${task.id}`, { method:'PUT', body: JSON.stringify({ ...task, dueDate: task.dueDate, isDone: !task.isDone }) }); if (button.dataset.action === 'archive') await api(`/api/tasks/${task.id}`, { method:'PUT', body: JSON.stringify({ ...task, dueDate: task.dueDate, archived: !task.archived }) }); if (button.dataset.action === 'delete' && confirm(`Delete “${task.title}”?`)) await api(`/api/tasks/${task.id}`, { method:'DELETE' }); await load(); } catch (error) { $('task-message').textContent = error.message; } }
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
    const alert = weather.kitesurfConditions ? '🪁 Kitesurf weather!' : 'Kitesurf alert: wind is 5 m/s or lower.';
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
  return { city: location.name || city, temperature: current.temperature_2m, feelsLike: current.apparent_temperature, windSpeed: current.wind_speed_10m, kitesurfConditions: current.wind_speed_10m > 5, description: describeWeatherCode(current.weather_code) };
}

function describeWeatherCode(code) { return ({ 0:'Clear sky', 1:'Mostly clear', 2:'Mostly clear', 3:'Overcast', 45:'Foggy', 48:'Foggy', 51:'Drizzle', 53:'Drizzle', 55:'Drizzle', 61:'Rain', 63:'Rain', 65:'Rain', 71:'Snow', 73:'Snow', 75:'Snow', 80:'Rain showers', 81:'Rain showers', 82:'Rain showers', 95:'Thunderstorm', 96:'Thunderstorm', 99:'Thunderstorm' })[code] || 'Unknown conditions'; }

async function addCategory() { const name = $('new-category').value.trim(); if (!name) return; try { await api('/api/categories', { method:'POST', body: JSON.stringify({ name }) }); $('new-category').value = ''; await load(); } catch (error) { $('task-message').textContent = error.message; } }

// These browser-only handlers preserve the same task interactions when the
// ASP.NET API is unavailable; the server-backed handlers remain the default.
async function saveTaskWithFallback(event) {
  event.preventDefault();
  $('form-message').textContent = '';
  const title = $('task-title').value.trim();
  if (!title) {
    $('form-message').textContent = 'Please enter a title.';
    return;
  }
  const task = { title, priority: Number($('task-priority').value), dueDate: $('task-due').value, categoryId: $('task-category').value ? Number($('task-category').value) : null, isDone: state.editing?.isDone || false, archived: state.editing?.archived || false };
  try {
    const id = $('task-id').value;
    await api(id ? `/api/tasks/${id}` : '/api/tasks', { method: id ? 'PUT' : 'POST', body: JSON.stringify(task) });
  } catch {
    const data = await localData();
    const id = Number($('task-id').value);
    if (id) {
      const index = data.tasks.findIndex(item => item.id === id);
      if (index >= 0) data.tasks[index] = { ...data.tasks[index], ...task };
    } else data.tasks.push({ ...task, id: nextLocalId(data.tasks) });
    await saveLocalData(data.tasks, data.categories);
  }
  $('task-dialog').close();
  await load();
}

async function handleTaskActionWithFallback(event) {
  const button = event.target.closest('[data-action]');
  if (!button) return;
  const task = state.tasks.find(item => item.id === Number(button.dataset.id));
  if (!task) return;
  if (button.dataset.action === 'edit') { openDialog(task); return; }
  if (button.dataset.action === 'delete' && !confirm(`Delete “${task.title}”?`)) return;
  try {
    if (button.dataset.action === 'toggle') await api(`/api/tasks/${task.id}`, { method:'PUT', body: JSON.stringify({ ...task, isDone: !task.isDone }) });
    if (button.dataset.action === 'archive') await api(`/api/tasks/${task.id}`, { method:'PUT', body: JSON.stringify({ ...task, archived: !task.archived }) });
    if (button.dataset.action === 'delete') await api(`/api/tasks/${task.id}`, { method:'DELETE' });
  } catch {
    const data = await localData();
    if (button.dataset.action === 'toggle') {
      const localTask = data.tasks.find(item => item.id === task.id);
      if (localTask) localTask.isDone = !localTask.isDone;
    } else if (button.dataset.action === 'archive') {
      const localTask = data.tasks.find(item => item.id === task.id);
      if (localTask) localTask.archived = !localTask.archived;
    } else data.tasks = data.tasks.filter(item => item.id !== task.id);
    await saveLocalData(data.tasks, data.categories);
  }
  await load();
}

async function addCategoryWithFallback() {
  const name = $('new-category').value.trim();
  if (!name) return;
  try { await api('/api/categories', { method:'POST', body: JSON.stringify({ name }) }); }
  catch {
    const data = await localData();
    data.categories.push({ id: nextLocalId(data.categories), name });
    await saveLocalData(data.tasks, data.categories);
  }
  $('new-category').value = '';
  await load();
}

async function bulkDoneAction(action) {
  // Bulk actions only target completed active tasks; archived tasks already have
  // their own view and must not be changed by these buttons.
  const done = state.tasks.filter(task => task.isDone && !task.archived);
  if (!done.length) return;
  if (action === 'delete' && !confirm(`Delete ${done.length} completed task${done.length === 1 ? '' : 's'}?`)) return;
  try {
    await Promise.all(done.map(task => api(`/api/tasks/${task.id}`, {
      method: action === 'delete' ? 'DELETE' : 'PUT',
      body: action === 'delete' ? undefined : JSON.stringify({ ...task, archived: true })
    })));
  } catch {
    const data = await localData();
    data.tasks = action === 'delete'
      ? data.tasks.filter(task => !(task.isDone && !task.archived))
      : data.tasks.map(task => task.isDone && !task.archived ? { ...task, archived: true } : task);
    await saveLocalData(data.tasks, data.categories);
  }
  await load();
}

document.querySelectorAll('.nav-button').forEach(button => button.addEventListener('click', () => showView(button.dataset.view)));
['open-tasks'].forEach(id => $(id).addEventListener('click', () => showView('tasks')));
['add-task','home-add'].forEach(id => $(id).addEventListener('click', () => openDialog()));
document.querySelectorAll('.task-list').forEach(list => list.addEventListener('click', handleTaskActionWithFallback));
$('task-form').addEventListener('submit', saveTaskWithFallback); $('task-priority').addEventListener('input', () => $('priority-output').value = $('task-priority').value); $('weather-load').addEventListener('click', loadWeather); $('weather-refresh').addEventListener('click', loadWeather);
$('filter-category').addEventListener('change', event => { state.categoryId = event.target.value; render(); }); $('sort-tasks').addEventListener('change', event => { state.sort = event.target.value; render(); }); $('add-category').addEventListener('click', addCategoryWithFallback);
$('archive-view').addEventListener('click', () => { state.showArchived = !state.showArchived; render(); });
$('hide-done').addEventListener('click', () => { state.hideDone = !state.hideDone; render(); });
$('archive-done').addEventListener('click', () => bulkDoneAction('archive'));
$('delete-done').addEventListener('click', () => bulkDoneAction('delete'));
load();
loadWeather();
setInterval(loadWeather, 15000);
