using System.ComponentModel.DataAnnotations;
using LifeManagementApp.Data;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Tests;

public class TodoRepositoryTests
{
    // This is an integration-style test: separate repository instances prove
    // that data really crosses DbContext boundaries and is not only in memory.
    [Fact]
    public async Task SavesReadsUpdatesAndDeletesAcrossContexts()
    {
        // Keep the test database under the test build output, inside the project.
        var path = Path.Combine(AppContext.BaseDirectory, $"test-{Guid.NewGuid()}.db");
        try
        {
            var factory = new TestFactory(path);
            var repository = new TodoRepository(factory);
            var category = new Category { Name = "Work" };
            await repository.AddCategoryAsync(category);
            var item = new TodoItem { Title = "First", Priority = 5, CategoryId = category.Id, Category = category };
            await repository.AddAsync(item);
            Assert.Same(category, item.Category);
            var reloaded = await new TodoRepository(factory).GetByIdAsync(item.Id);
            Assert.Equal("Work", reloaded!.Category!.Name);
            reloaded.Title = "Updated";
            await repository.UpdateAsync(reloaded);
            Assert.Equal("Updated", (await repository.GetByIdAsync(item.Id))!.Title);
            await repository.DeleteAsync(item.Id);
            Assert.Empty(await repository.GetAllAsync());
            await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.DeleteAsync(item.Id));
            await Assert.ThrowsAsync<ValidationException>(() => repository.AddAsync(new TodoItem { Title = " " }));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private sealed class TestFactory(string path) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options);
    }
}



