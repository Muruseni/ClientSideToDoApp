using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class EditTodoViewModel(ITodoRepository repository, INavigationService navigation) : ObservableObject
{
    private TodoItem? item;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private int priority = 3;
    [ObservableProperty] private DateTime dueDate = DateTime.Today;
    [ObservableProperty] private string errorMessage = string.Empty;

    public async Task LoadAsync(int id)
    {
        try
        {
            item = await repository.GetByIdAsync(id);
            if (item is null) { ErrorMessage = "Task not found."; return; }
            Title = item.Title; Priority = item.Priority; DueDate = item.DueDate;
        }
        catch (Exception) { ErrorMessage = "Could not load this task."; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (item is null) { ErrorMessage = "Task not found."; return; }
        if (string.IsNullOrWhiteSpace(Title)) { ErrorMessage = "Please enter a title."; return; }
        item.Title = Title.Trim(); item.Priority = Priority; item.DueDate = DueDate;
        try { await repository.UpdateAsync(item); await navigation.GoBackAsync(); }
        catch (Exception) { ErrorMessage = "Could not save the task. Please try again."; }
    }
}
