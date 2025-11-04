using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using AgriPosPoC.Core.Data;
using AgriPosPoC.Client;
using MudBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
});

// Register the HttpClientFactory and a named client
builder.Services.AddHttpClient("ServerApi", client =>
{
    // This will be the URL of your running Server project
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// Register the Offline DbContext for SQLite
builder.Services.AddDbContextFactory<OfflineDbContext>(options =>
    options.UseSqlite("Data Source=agripos.db"));

// Register the SyncService as a Singleton
builder.Services.AddSingleton<SyncService>();

// Build the app
var app = builder.Build();

// Create a scope to initialize the database ONCE at startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OfflineDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

// Run the app
await app.RunAsync();