using System.ComponentModel.DataAnnotations;
using LifeManagementApp.Data;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// Keep the browser host on the same SQLite-backed repository as the MAUI client.
// LIFE_APP_DATABASE_PATH makes isolated test runs possible without changing source code.
var databasePath = Environment.GetEnvironmentVariable("LIFE_APP_DATABASE_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "tasks.db");
var databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
if (!string.IsNullOrWhiteSpace(databaseDirectory)) Directory.CreateDirectory(databaseDirectory);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>();

var app = builder.Build();

// Create the SQLite schema before accepting requests so a fresh deployment has
// a ready database even when the first request is a weather or static-file call.
await using (var startupScope = app.Services.CreateAsyncScope())
{
    var repository = startupScope.ServiceProvider.GetRequiredService<ITodoRepository>();
    await repository.InitializeAsync();
}

// Static files contain the browser shell; the API endpoints below provide its data contract.
app.UseDefaultFiles();
app.UseStaticFiles();

// Return DTO-shaped task records so EF navigation properties do not create JSON cycles.
app.MapGet("/api/tasks", async (ITodoRepository repository) =>
    Results.Ok((await repository.GetAllAsync()).Select(ToTaskResponse)));

app.MapGet("/api/tasks/{id:int}", async (int id, ITodoRepository repository) =>
{
    var item = await repository.GetByIdAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(ToTaskResponse(item));
});

app.MapPost("/api/tasks", async (TaskRequest request, ITodoRepository repository) =>
{
    var item = new TodoItem
    {
        Title = request.Title?.Trim() ?? string.Empty,
        Priority = request.Priority,
        DueDate = request.DueDate?.Date ?? DateTime.Today,
        CategoryId = request.CategoryId
    };

    // Validate through the shared repository so browser and MAUI rules cannot diverge.
    try
    {
        await repository.AddAsync(item);
        return Results.Created($"/api/tasks/{item.Id}", ToTaskResponse(await repository.GetByIdAsync(item.Id) ?? item));
    }
    catch (ValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["task"] = [exception.Message] });
    }
});

app.MapPut("/api/tasks/{id:int}", async (int id, TaskRequest request, ITodoRepository repository) =>
{
    var item = await repository.GetByIdAsync(id);
    if (item is null) return Results.NotFound();

    item.Title = request.Title?.Trim() ?? string.Empty;
    item.Priority = request.Priority;
    item.DueDate = request.DueDate?.Date ?? DateTime.Today;
    item.CategoryId = request.CategoryId;
    item.IsDone = request.IsDone;

    try
    {
        await repository.UpdateAsync(item);
        return Results.Ok(ToTaskResponse(await repository.GetByIdAsync(id) ?? item));
    }
    catch (ValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["task"] = [exception.Message] });
    }
});

app.MapDelete("/api/tasks/{id:int}", async (int id, ITodoRepository repository) =>
{
    try
    {
        await repository.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException) { return Results.NotFound(); }
});

app.MapGet("/api/categories", async (ITodoRepository repository) =>
    Results.Ok(await repository.GetCategoriesAsync()));

app.MapPost("/api/categories", async (CategoryRequest request, ITodoRepository repository) =>
{
    var category = new Category { Name = request.Name?.Trim() ?? string.Empty };
    try
    {
        await repository.AddCategoryAsync(category);
        return Results.Created($"/api/categories/{category.Id}", category);
    }
    catch (ValidationException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["category"] = [exception.Message] });
    }
});

app.MapDelete("/api/categories/{id:int}", async (int id, AppDbContext db) =>
{
    await db.Database.EnsureCreatedAsync();
    var category = await db.Categories.FindAsync(id);
    if (category is null) return Results.NotFound();
    // Refuse destructive relationship changes; callers must reassign tasks first.
    if (await db.TodoItems.AnyAsync(item => item.CategoryId == id)) return Results.Conflict(new { message = "Category still has tasks." });
    db.Categories.Remove(category);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/weather", async (string city, IWeatherService weatherService, CancellationToken cancellationToken) =>
{
    try { return Results.Ok(await weatherService.GetCurrentAsync(city, cancellationToken)); }
    catch (Exception exception) { return Results.BadRequest(new { message = exception.Message }); }
});

app.MapFallbackToFile("index.html");
app.Run();

// The response deliberately exposes the category name but not Category.TodoItems.
static TaskResponse ToTaskResponse(TodoItem item) => new(
    item.Id, item.Title, item.Priority, item.DueDate, item.IsDone,
    item.CategoryId, item.Category?.Name);

record TaskRequest(string? Title, int Priority, DateTime? DueDate, int? CategoryId, bool IsDone = false);
record CategoryRequest(string? Name);
record TaskResponse(int Id, string Title, int Priority, DateTime DueDate, bool IsDone, int? CategoryId, string? CategoryName);
