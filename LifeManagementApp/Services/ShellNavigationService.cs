namespace LifeManagementApp.Services;

public class ShellNavigationService : INavigationService
{
    public Task GoToAddTodoAsync() => Shell.Current.GoToAsync("add");
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
