using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoListViewModel(ITodoRepository repository, INavigationService navigation) : ObservableObject
{
    // allItems is the current repository snapshot; Items is the filtered/sorted projection shown by XAML.
    public ObservableCollection<TodoItem> Items { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public IReadOnlyList<string> SortOptions { get; } = ["Priority", "Due date", "Title"];
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private string selectedSort = "Priority";
    [ObservableProperty] private string newCategoryName = string.Empty;
    [ObservableProperty] private bool showArchived;
    [ObservableProperty] private bool hideDone;

    private IReadOnlyList<TodoItem> allItems = [];

    public string ArchiveViewButtonText => ShowArchived ? "Hide Archived Tasks" : "View Archived Tasks";
    public string HideDoneButtonText => HideDone ? "Show Done" : "Hide Done";

    partial void OnSelectedCategoryChanged(Category? value) => ApplyFilters();
    partial void OnSelectedSortChanged(string value) => ApplyFilters();
    partial void OnShowArchivedChanged(bool value)
    {
        OnPropertyChanged(nameof(ArchiveViewButtonText));
        ApplyFilters();
    }
    partial void OnHideDoneChanged(bool value)
    {
        OnPropertyChanged(nameof(HideDoneButtonText));
        ApplyFilters();
    }

    public async Task RefreshAsync()
    {
        try
        {
            allItems = await repository.GetAllAsync();
            Categories.Clear();
            foreach (var category in await repository.GetCategoriesAsync()) Categories.Add(category);
            ApplyFilters();
            ErrorMessage = string.Empty;
        }
        catch (Exception) { ErrorMessage = "Could not load tasks. Please try again."; }
    }

    private void ApplyFilters()
    {
        // Reapply both choices together so changing one control never discards the other.
        var filtered = allItems.Where(item => item.Archived == ShowArchived
            && (ShowArchived || !HideDone || !item.IsDone)
            && (SelectedCategory is null || item.CategoryId == SelectedCategory.Id));
        filtered = SelectedSort switch
        {
            "Due date" => filtered.OrderBy(item => item.DueDate).ThenByDescending(item => item.Priority),
            "Title" => filtered.OrderBy(item => item.Title),
            _ => filtered.OrderByDescending(item => item.Priority).ThenBy(item => item.DueDate)
        };

        Items.Clear();
        foreach (var item in filtered) Items.Add(item);
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        // Trim before persistence so visually identical categories are not created accidentally.
        var name = NewCategoryName.Trim();
        if (string.IsNullOrWhiteSpace(name)) { ErrorMessage = "Enter a category name."; return; }

        try
        {
            var category = new Category { Name = name };
            await repository.AddCategoryAsync(category);
            Categories.Add(category);
            NewCategoryName = string.Empty;
            SelectedCategory = category;
            ErrorMessage = string.Empty;
        }
        catch (Exception) { ErrorMessage = "Could not create that category."; }
    }

    [RelayCommand] private Task AddAsync() => navigation.GoToAddTodoAsync();
    [RelayCommand] private Task EditAsync(TodoItem item) => navigation.GoToEditTodoAsync(item.Id);

    [RelayCommand]
    private void ToggleArchivedView() => ShowArchived = !ShowArchived;

    [RelayCommand]
    private void ToggleHideDone() => HideDone = !HideDone;

    [RelayCommand]
    private async Task ArchiveAsync(TodoItem item)
    {
        var previous = item.Archived;
        item.Archived = !previous;
        try
        {
            await repository.UpdateAsync(item);
            await RefreshAsync();
        }
        catch (Exception)
        {
            item.Archived = previous;
            ErrorMessage = "Could not update the archive status.";
        }
    }

    [RelayCommand]
    private async Task ArchiveDoneAsync()
    {
        try
        {
            foreach (var item in allItems.Where(item => item.IsDone && !item.Archived).ToList())
            {
                item.Archived = true;
                await repository.UpdateAsync(item);
            }
            await RefreshAsync();
            ErrorMessage = string.Empty;
        }
        catch (Exception)
        {
            ErrorMessage = "Could not archive completed tasks.";
        }
    }

    [RelayCommand]
    private async Task DeleteDoneAsync()
    {
        try
        {
            foreach (var item in allItems.Where(item => item.IsDone && !item.Archived).ToList())
            {
                await repository.DeleteAsync(item.Id);
            }
            await RefreshAsync();
            ErrorMessage = string.Empty;
        }
        catch (Exception)
        {
            ErrorMessage = "Could not delete completed tasks.";
        }
    }

    [RelayCommand]
    private async Task ToggleDoneAsync(TodoItem item)
    {
        var previous = item.IsDone;
        item.IsDone = !previous;
        try
        {
            await repository.UpdateAsync(item);
            await RefreshAsync();
        }
        catch (Exception)
        {
            item.IsDone = previous;
            ErrorMessage = "Could not update the task.";
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(TodoItem item)
    {
        try
        {
            await repository.DeleteAsync(item.Id);
            Items.Remove(item);
            ErrorMessage = string.Empty;
        }
        catch (Exception)
        {
            ErrorMessage = "Could not delete the task.";
        }
    }
}
