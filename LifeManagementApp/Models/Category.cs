using System.ComponentModel.DataAnnotations;

namespace LifeManagementApp.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
}
