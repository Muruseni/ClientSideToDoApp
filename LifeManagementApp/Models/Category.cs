using System.ComponentModel.DataAnnotations;

namespace LifeManagementApp.Models;

public class Category
{
    // EF supplies the identifier when a new category is saved.
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    // The collection is used by EF for relationship tracking; list screens use CategoryId.
    public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
}
