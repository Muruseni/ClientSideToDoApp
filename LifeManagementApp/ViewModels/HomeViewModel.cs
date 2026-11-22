using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class HomeViewModel(ITodoRepository repository, INavigationService navigation, IWeatherService? weatherService = null) : ObservableObject
{
    // The optional weather service keeps task previews testable without network access.
    public ObservableCollection<TodoItem> Items { get; } = [];
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string city = "Vaasa";
    [ObservableProperty] private WeatherSnapshot? weather;
    [ObservableProperty] private bool isWeatherBusy;
    [ObservableProperty] private string weatherError = string.Empty;
    public string SupportiveMessage { get; } = SupportiveMessageProvider.GetRandom();

    public async Task RefreshAsync()
    {
        try
        {
            var items = await repository.GetAllAsync();
            Items.Clear();
            // The home page is intentionally a short priority preview, not a second task list.
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

    public Task RefreshWeatherAsync() => LoadWeatherAsync();

    [RelayCommand]
    private Task OpenTasksAsync() => navigation.GoToTodoListAsync();

    [RelayCommand]
    private async Task LoadWeatherAsync()
    {
        // Prevent duplicate requests when refresh is tapped repeatedly while the network is busy.
        if (weatherService is null || IsWeatherBusy) return;
        IsWeatherBusy = true;
        WeatherError = string.Empty;
        try { Weather = await weatherService.GetCurrentAsync(City); }
        catch (Exception) { WeatherError = "Weather is unavailable right now."; }
        finally { IsWeatherBusy = false; }
    }
}
