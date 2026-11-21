using System.Collections.ObjectModel;
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
    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<Category> Categories { get; } = [];

    public async Task LoadAsync()
    {
        try
        {
            Categories.Clear();
            foreach (var category in await repository.GetCategoriesAsync()) Categories.Add(category);
            ErrorMessage = string.Empty;
        }
        catch (Exception)
        {
            ErrorMessage = "Could not load categories. You can still save without one.";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Title)) { ErrorMessage = "Please enter a title."; return; }
        if (Priority is < 1 or > 5) { ErrorMessage = "Priority must be between 1 and 5."; return; }

        IsBusy = true;
        try
        {
            await repository.AddAsync(new TodoItem
            {
                Title = Title.Trim(),
                Priority = Priority,
                DueDate = DueDate,
                CategoryId = SelectedCategory?.Id,
                Category = SelectedCategory
            });
            await navigation.GoBackAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "Could not save the task. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
