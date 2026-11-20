using LifeManagementApp.ViewModels;
using LifeManagementApp.Views;
using LifeManagementApp.Data;
using LifeManagementApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeManagementApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// The database stays in the app's private data directory.
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, "tasks.db");
		builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
		builder.Services.AddSingleton<ITodoRepository, TodoRepository>();
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

		// Pages and view models are transient so navigation receives fresh UI state.
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<TodoListPage>();
		builder.Services.AddTransient<AddTodoPage>();
		builder.Services.AddTransient<TodoListViewModel>();
		builder.Services.AddTransient<AddTodoViewModel>();
		builder.Services.AddTransient<EditTodoPage>();
		builder.Services.AddTransient<EditTodoViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}





