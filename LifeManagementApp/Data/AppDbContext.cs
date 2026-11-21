using LifeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeManagementApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // These sets are intentionally small: the repository owns query and persistence rules.
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Removing a category should leave its tasks usable as uncategorized tasks.
        modelBuilder.Entity<TodoItem>()
            .HasOne(item => item.Category)
            .WithMany(category => category.TodoItems)
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
