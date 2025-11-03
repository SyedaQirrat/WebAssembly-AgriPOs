// AgriPosPoC.Server/Program.cs

using AgriPosPoC.Core.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Add Services ---

// Add DbContext for ONLINE (Azure SQL)
var connectionString = builder.Configuration.GetConnectionString("OnlineConnection");
builder.Services.AddDbContext<OnlineDbContext>(options =>
    options.UseSqlServer(connectionString, b =>
    {
        b.MigrationsAssembly("AgriPosPoC.Server"); // <-- ADD THIS
    }));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// **IMPORTANT: Add CORS Policy**
// This allows your Client app to call your Server app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("https://localhost:7095") // <-- Change to Client's URL
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- 2. Configure HTTP Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowClient"); // **IMPORTANT: Use the CORS policy**

app.UseAuthorization();
app.MapControllers();
app.Run();