namespace LifeManagementApp.Services;

public class ShellNavigationService : INavigationService
{
    // Route names match AppShell and the detail routes registered in AppShell.xaml.cs.
    public Task GoToAddTodoAsync() => Shell.Current.GoToAsync("add");
    public Task GoToEditTodoAsync(int id) => Shell.Current.GoToAsync($"edit?id={id}");
    public Task GoToTodoListAsync() => Shell.Current.GoToAsync("todos");
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
