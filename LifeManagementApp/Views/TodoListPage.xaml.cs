using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Views;

public partial class TodoListPage : ContentPage
{
    private readonly TodoListViewModel viewModel;
    public TodoListPage(TodoListViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Reload categories and tasks after returning from add/edit flows.
        await viewModel.RefreshAsync();
    }
}
