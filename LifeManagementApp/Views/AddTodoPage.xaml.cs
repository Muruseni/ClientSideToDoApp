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
        await ViewModel.LoadAsync();
    }
}
