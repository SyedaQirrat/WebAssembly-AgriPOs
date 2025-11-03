// /Program.cs

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using AgriPosPoC.Core.Data;
using AgriPosPoC.Client;
using MudBlazor; // <-- ADD THIS LINE

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 1. Add MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight; // <-- This will now work
    config.SnackbarConfiguration.PreventDuplicates = false;
});

// 2. Configure HttpClient to talk to the Server API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7123") }); // <-- IMPORTANT: Use your Server's URL

// 3. Add Offline DbContext for SQLite
builder.Services.AddDbContextFactory<OfflineDbContext>(options =>
    options.UseSqlite("Data Source=agripos.db"));

// 4. Add the SyncService as a singleton
builder.Services.AddSingleton<SyncService>();

await builder.Build().RunAsync();