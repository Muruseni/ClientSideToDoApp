namespace LifeManagementApp.Services;

public interface INavigationService
{
    // View models depend on this abstraction so their tests never need a real Shell.
    Task GoToAddTodoAsync();
    Task GoToEditTodoAsync(int id);
    Task GoToTodoListAsync();
    Task GoBackAsync();
}
