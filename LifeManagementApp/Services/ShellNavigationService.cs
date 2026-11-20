namespace LifeManagementApp.Services;

public class ShellNavigationService : INavigationService
{
    public Task GoToAddTodoAsync() => Shell.Current.GoToAsync("add");
    public Task GoToEditTodoAsync(int id) => Shell.Current.GoToAsync($"edit?id={id}");
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
