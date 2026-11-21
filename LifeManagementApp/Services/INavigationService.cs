namespace LifeManagementApp.Services;

public interface INavigationService
{
    Task GoToAddTodoAsync();
    Task GoToEditTodoAsync(int id);
    Task GoToTodoListAsync();
    Task GoBackAsync();
}
