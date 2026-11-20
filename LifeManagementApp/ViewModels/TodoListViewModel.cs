using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeManagementApp.Services;

namespace LifeManagementApp.ViewModels;

public partial class TodoListViewModel(INavigationService navigation) : ObservableObject
{
    [RelayCommand]
    private Task AddAsync() => navigation.GoToAddTodoAsync();
}
