using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoListViewModel(ITodoRepository repository, INavigationService navigation) : ObservableObject
{
    public ObservableCollection<TodoItem> Items { get; } = [];
    [ObservableProperty] private string errorMessage = string.Empty;

    public async Task RefreshAsync()
    {
        try
        {
            var items = await repository.GetAllAsync();
            Items.Clear();
            foreach (var item in items.OrderByDescending(item => item.Priority).ThenBy(item => item.DueDate)) Items.Add(item);
            ErrorMessage = string.Empty;
        }
        catch (Exception) { ErrorMessage = "Could not load tasks. Please try again."; }
    }

    [RelayCommand] private Task AddAsync() => navigation.GoToAddTodoAsync();
    [RelayCommand] private Task EditAsync(TodoItem item) => navigation.GoToEditTodoAsync(item.Id);
}
