namespace LifeManagementApp.Services;

public interface INavigationService
{
    Task GoToAddTodoAsync();
    Task GoBackAsync();
}
