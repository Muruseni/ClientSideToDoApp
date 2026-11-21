# Life Management App

A .NET 9 MAUI course project for managing tasks and viewing weather. The current foundation targets Windows; mobile targets can be added once their workloads and device testing are available.

## Build

Install the .NET 9 SDK and the `maui-windows` workload, then run:

```powershell
dotnet build LifeManagementApp/LifeManagementApp.csproj -f net9.0-windows10.0.19041.0
```

The MAUI client now contains task storage, task CRUD, categories, sorting/filtering, and a weather card. The browser client is available in [LifeManagementApp.Web](LifeManagementApp.Web) for machines where Windows Application Control blocks the MAUI executable.

## Storage

The SQLite repository uses an EF Core Code First context. The database path is in the app data directory and the repository exposes asynchronous operations. The repository initializes the database on first use. The initial schema uses `EnsureCreated`; it does not upgrade existing databases. Before changing a released schema, introduce and apply EF Core migrations instead of relying on `EnsureCreated`.

Run model and repository tests with `dotnet test LifeManagementApp.Tests/LifeManagementApp.Tests.csproj`.

## Browser version

The repository now includes an ASP.NET Core web version in [LifeManagementApp.Web](LifeManagementApp.Web). It reuses the task models, SQLite repository, and weather service, but runs in a browser instead of launching a Windows executable:

```powershell
dotnet run --project LifeManagementApp.Web/LifeManagementApp.Web.csproj
```

Open the local URL printed by ASP.NET Core, normally `http://localhost:5000` or `https://localhost:7000`. The web host creates its SQLite database beside the web app unless `LIFE_APP_DATABASE_PATH` is set.

The task list supports priority, due-date and title sorting, category filtering, and category creation. Weather wind speed is reported in metres per second; the kitesurf alert is active at 5 m/s or above.


For isolated development checks, set `LIFE_APP_DATABASE_PATH` to an absolute SQLite file path before starting the app. Without it, the normal app-data location is used.
