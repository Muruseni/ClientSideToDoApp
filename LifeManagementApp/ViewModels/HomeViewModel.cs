using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class HomeViewModel(ITodoRepository repository, INavigationService navigation, IWeatherService? weatherService = null) : ObservableObject
{
    public ObservableCollection<TodoItem> Items { get; } = [];
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string city = "Kyiv";
    [ObservableProperty] private WeatherSnapshot? weather;
    [ObservableProperty] private bool isWeatherBusy;
    [ObservableProperty] private string weatherError = string.Empty;

    public async Task RefreshAsync()
    {
        try
        {
            var items = await repository.GetAllAsync();
            Items.Clear();
            foreach (var item in items
                .Where(item => !item.IsDone)
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.DueDate)
                .Take(3))
            {
                Items.Add(item);
            }

            ErrorMessage = string.Empty;
            if (weatherService is not null) await LoadWeatherAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "Could not load your tasks.";
        }
    }

    [RelayCommand]
    private Task OpenTasksAsync() => navigation.GoToTodoListAsync();

    [RelayCommand]
    private async Task LoadWeatherAsync()
    {
        if (weatherService is null || IsWeatherBusy) return;
        IsWeatherBusy = true;
        WeatherError = string.Empty;
        try { Weather = await weatherService.GetCurrentAsync(City); }
        catch (Exception) { WeatherError = "Weather is unavailable right now."; }
        finally { IsWeatherBusy = false; }
    }
}
