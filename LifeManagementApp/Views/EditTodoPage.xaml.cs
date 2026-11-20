using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Views;

public partial class EditTodoPage : ContentPage, IQueryAttributable
{
    private readonly EditTodoViewModel viewModel;
    public EditTodoPage(EditTodoViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    // Shell passes the identifier; the view model loads a fresh copy from storage.
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var value) && int.TryParse(value?.ToString(), out var id))
            await viewModel.LoadAsync(id);
    }
}
