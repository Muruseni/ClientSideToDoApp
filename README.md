# Life Management App

A .NET 9 MAUI course project for managing tasks and viewing weather. The current foundation targets Windows; mobile targets can be added once their workloads and device testing are available.

## Build

Install the .NET 9 SDK and the `maui-windows` workload, then run:

```powershell
dotnet build LifeManagementApp/LifeManagementApp.csproj -f net9.0-windows10.0.19041.0
```

The app currently contains the standard MAUI starter screen. Task storage and weather are planned in the project issues.

## Storage

The SQLite repository uses an EF Core Code First context. The database path is in the app data directory and the repository exposes asynchronous operations. The repository initializes the database on first use. The initial schema uses `EnsureCreated`; it does not upgrade existing databases. Before changing a released schema, introduce and apply EF Core migrations instead of relying on `EnsureCreated`.

Run model and repository tests with `dotnet test LifeManagementApp.Tests/LifeManagementApp.Tests.csproj`.

