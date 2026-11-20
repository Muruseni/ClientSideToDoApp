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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

