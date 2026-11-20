using LifeManagementApp.Views;

namespace LifeManagementApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Detail pages are pushed on top of a Shell tab rather than shown in the flyout.
        Routing.RegisterRoute("add", typeof(AddTodoPage));
    }
}
