namespace LifeManagementApp;

public partial class App : Application
{
	public App()
	{
		// MAUI creates the visual tree from AppShell so every platform shares navigation.
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Keep the window creation platform-neutral; AppShell owns the actual page routes.
		return new Window(new AppShell());
	}
}
