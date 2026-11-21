using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface ITodoRepository
{
    // Initialization is explicit so UI and web callers can share the same lazy database setup.
    Task InitializeAsync();
    Task<IReadOnlyList<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(int id);
    Task AddAsync(TodoItem item);
    Task UpdateAsync(TodoItem item);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task AddCategoryAsync(Category category);
}
