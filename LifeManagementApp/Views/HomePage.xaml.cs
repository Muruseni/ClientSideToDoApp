using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel viewModel;
    private CancellationTokenSource? weatherRefreshCancellation;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Refresh on each appearance so returning from a task edit shows current data.
        await viewModel.RefreshAsync();
        weatherRefreshCancellation = new CancellationTokenSource();
        _ = RefreshWeatherPeriodicallyAsync(weatherRefreshCancellation.Token);
    }

    protected override void OnDisappearing()
    {
        weatherRefreshCancellation?.Cancel();
        weatherRefreshCancellation?.Dispose();
        weatherRefreshCancellation = null;
        base.OnDisappearing();
    }

    private async Task RefreshWeatherPeriodicallyAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await viewModel.RefreshWeatherAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The page no longer needs weather updates.
        }
    }
}
