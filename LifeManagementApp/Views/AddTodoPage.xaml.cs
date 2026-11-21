using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Views;

public partial class AddTodoPage : ContentPage
{
    private AddTodoViewModel ViewModel => (AddTodoViewModel)BindingContext;

    public AddTodoPage(AddTodoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Categories may have changed since the page was first constructed.
        await ViewModel.LoadAsync();
    }
}
