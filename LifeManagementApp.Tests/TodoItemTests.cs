using System.ComponentModel.DataAnnotations;
using LifeManagementApp.Models;

namespace LifeManagementApp.Tests;

public class TodoItemTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRejectsBlankTitles(string title)
    {
        var item = new TodoItem { Title = title };
        Assert.Throws<ValidationException>(item.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ValidateRejectsOutOfRangePriority(int priority)
    {
        var item = new TodoItem { Title = "Task", Priority = priority };
        Assert.Throws<ValidationException>(item.Validate);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ValidateAcceptsPriorityBoundaries(int priority)
    {
        var item = new TodoItem { Title = "Task", Priority = priority };
        item.Validate();
    }
}
