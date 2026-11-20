using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoListViewModel(ITodoRepository repository, INavigationService navigation) : ObservableObject
{
    private IReadOnlyList<TodoItem> loadedItems = [];
    public ObservableCollection<TodoItem> Items { get; } = [];
    public IReadOnlyList<string> SortOptions { get; } = ["Priority", "Due date"];
    [ObservableProperty] private int selectedSortIndex;
    [ObservableProperty] private string errorMessage = string.Empty;

    // Reorder the already loaded items when the user changes the sort option.
    partial void OnSelectedSortIndexChanged(int value) => ApplySort();

    public async Task RefreshAsync()
    {
        try
        {
            loadedItems = await repository.GetAllAsync();
            ApplySort();
            ErrorMessage = string.Empty;
        }
        catch (Exception) { ErrorMessage = "Could not load tasks. Please try again."; }
    }

    private void ApplySort()
    {
        var sorted = SelectedSortIndex == 1
            ? loadedItems.OrderBy(item => item.DueDate).ThenByDescending(item => item.Priority).ThenBy(item => item.Id)
            : loadedItems.OrderByDescending(item => item.Priority).ThenBy(item => item.DueDate).ThenBy(item => item.Id);
        Items.Clear();
        foreach (var item in sorted) Items.Add(item);
    }

    [RelayCommand] private Task AddAsync() => navigation.GoToAddTodoAsync();
    [RelayCommand] private Task EditAsync(TodoItem item) => navigation.GoToEditTodoAsync(item.Id);
}
