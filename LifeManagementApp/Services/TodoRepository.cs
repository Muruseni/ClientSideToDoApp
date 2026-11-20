using System.ComponentModel.DataAnnotations;
using LifeManagementApp.Data;
using LifeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Services;

public class TodoRepository(IDbContextFactory<AppDbContext> contextFactory) : ITodoRepository
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool initialized;

    // Initialize once, and allow another attempt if database creation fails.
    public async Task InitializeAsync()
    {
        await initializationLock.WaitAsync();
        try
        {
            if (initialized) return;
            await using var db = await contextFactory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
            initialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync()
    {
        await InitializeAsync();
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.TodoItems.AsNoTracking().Include(item => item.Category).ToListAsync();
    }

    public async Task<TodoItem?> GetByIdAsync(int id)
    {
        await InitializeAsync();
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.TodoItems.AsNoTracking().Include(item => item.Category).FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task AddAsync(TodoItem item)
    {
        item.Validate();
        await InitializeAsync();
        await using var db = await contextFactory.CreateDbContextAsync();
        // Attach only the foreign key; an existing category must not be inserted again.
        item.Category = null;
        db.TodoItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TodoItem item)
    {
        item.Validate();
        await InitializeAsync();
        await using var db = await contextFactory.CreateDbContextAsync();
        var existing = await db.TodoItems.FindAsync(item.Id) ?? throw new KeyNotFoundException("Task not found.");
        db.Entry(existing).CurrentValues.SetValues(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await InitializeAsync();
        await using var db = await contextFactory.CreateDbContextAsync();
        var item = await db.TodoItems.FindAsync(id) ?? throw new KeyNotFoundException("Task not found.");
        db.TodoItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        await InitializeAsync();
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync();
    }

    public async Task AddCategoryAsync(Category category)
    {
        Validator.ValidateObject(category, new ValidationContext(category), validateAllProperties: true);
        await InitializeAsync();
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Categories.Add(category);
        await db.SaveChangesAsync();
    }
}

