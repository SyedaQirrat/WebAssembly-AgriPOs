using AgriPosPoC.Core.Data;
using AgriPosPoC.Server.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Add Services ---

builder.Services.AddSignalR();
var connectionString = builder.Configuration.GetConnectionString("OnlineConnection");
builder.Services.AddDbContext<OnlineDbContext>(options =>
    options.UseSqlServer(connectionString, b =>
    {
        b.MigrationsAssembly("AgriPosPoC.Server");
    }));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        // Use the Client's URL from its launchSettings.json
        policy.WithOrigins("https://localhost:7102") // <-- This must match Client's URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// --- 2. Configure HTTP Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // This will now work because of the new pckage
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.UseCors("AllowClient");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllers();
app.MapHub<SyncHub>("/synchub");

// This serves the Blazor app as the default
app.MapFallbackToFile("index.html");

app.Run();