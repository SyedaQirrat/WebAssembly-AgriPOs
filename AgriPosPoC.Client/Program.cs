using AgriPosPoC.Client;
using AgriPosPoC.Core.Data;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using Blazor.IndexedDB;
using TG.Blazor.IndexedDB;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Set the API's base address
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7102")
});

//  Register IndexedDB BEFORE building the app

builder.Services.AddIndexedDB(dbStore =>
{
    dbStore.DbName = "AgriPosDB";
    dbStore.Version = 1;
    // Define stores and indexes here
});

// Add your sync service
builder.Services.AddScoped<SyncService>();

//  Now build and run the app
await builder.Build().RunAsync();
