using System.ComponentModel.DataAnnotations;

namespace LifeManagementApp.Models;

public class TodoItem
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    // Larger numbers represent higher priority throughout the application.
    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    public DateTime DueDate { get; set; } = DateTime.Today;
    public bool IsDone { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}
