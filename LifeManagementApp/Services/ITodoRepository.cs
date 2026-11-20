using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface ITodoRepository
{
    Task InitializeAsync();
    Task<IReadOnlyList<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(int id);
    Task AddAsync(TodoItem item);
    Task UpdateAsync(TodoItem item);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task AddCategoryAsync(Category category);
}
