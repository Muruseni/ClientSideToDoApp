using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class AddTodoViewModel(ITodoRepository repository, INavigationService navigation) : ObservableObject
{
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private int priority = 3;
    [ObservableProperty] private DateTime dueDate = DateTime.Today;
    [ObservableProperty] private string errorMessage = string.Empty;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title)) { ErrorMessage = "Please enter a title."; return; }
        try
        {
            await repository.AddAsync(new TodoItem { Title = Title.Trim(), Priority = Priority, DueDate = DueDate });
            await navigation.GoBackAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "Could not save the task. Please try again.";
        }
    }
}


