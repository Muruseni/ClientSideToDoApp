# Life Management App

A .NET 9 MAUI course project for managing tasks, categories, priorities, due dates, and weather. The repository includes both the original MAUI client and a browser client.

## Safest way to run the browser client on Windows

This project was blocked on one Windows installation by Smart App Control. The Code Integrity log reported that `LifeManagementApp.Web.dll` did not meet the enterprise signing requirements. The safe development workaround is to run only the static browser files with Node; this does not disable Windows security or launch the blocked .NET executable.

From the repository root, run:

```powershell
node .\LifeManagementApp.Web\static-server.mjs
```

Open [http://127.0.0.1:5087](http://127.0.0.1:5087) in a browser. The static server is implemented in [LifeManagementApp.Web/static-server.mjs](LifeManagementApp.Web/static-server.mjs) and serves only [LifeManagementApp.Web/wwwroot](LifeManagementApp.Web/wwwroot).

### Browser-only behavior

When the ASP.NET API is unavailable, [app.js](LifeManagementApp.Web/wwwroot/app.js) automatically switches to offline mode:

- Tasks and categories are stored in the browser's `localStorage`.
- Weather uses the direct Open-Meteo geocoding and forecast endpoints.
- The kitesurf alert is active at wind speeds of 5 m/s or higher.
- Data in offline mode is local to that browser and is not written to the SQLite database.

The browser client still prefers the ASP.NET API when it is available, so the same UI can use server-backed SQLite persistence during normal development.

## Full ASP.NET web host

If Windows permits the .NET web host, run:

```powershell
dotnet run --project .\LifeManagementApp.Web\LifeManagementApp.Web.csproj --urls http://127.0.0.1:5087
```

The web host reuses the task models, SQLite repository, and weather service. Its API provides task and category persistence. Set `LIFE_APP_DATABASE_PATH` to an absolute SQLite path when an isolated database is needed.

If Code Integrity blocks the web host, do not use Defender exclusions or registry bypasses. An administrator must approve the application through the device's application-control policy, or all application binaries must be signed with a certificate trusted by Windows. Smart App Control does not provide a safe per-project trust command.

## MAUI build

Install the .NET 9 SDK and the `maui-windows` workload, then run:

```powershell
dotnet build .\LifeManagementApp\LifeManagementApp.csproj -f net9.0-windows10.0.19041.0
```

The MAUI client contains task storage, task CRUD, categories, sorting/filtering, and a weather card. It may require administrator approval on devices enforcing enterprise signing policies.

## Testing and development checks

Run the test project with:

```powershell
dotnet test .\LifeManagementApp.Tests\LifeManagementApp.Tests.csproj
```

If the test host is also blocked by Code Integrity, the project can still be compiled; run the tests on an approved development machine or ask the administrator to approve the test binaries.

For front-end changes, validate the JavaScript before opening the browser client:

```powershell
node --check .\LifeManagementApp.Web\wwwroot\app.js
```

The SQLite repository uses an EF Core Code First context. The initial schema uses `EnsureCreated`; before changing a released schema, introduce and apply EF Core migrations instead of relying on `EnsureCreated`.
