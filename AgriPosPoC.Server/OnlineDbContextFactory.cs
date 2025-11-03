using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using AgriPosPoC.Core.Data;

namespace AgriPosPoC.Server
{
    /*
     * This factory is used by the 'dotnet ef' command-line tools.
     * It manually builds the configuration and connection string
     * so that the tool can create migrations and update the database
     * without needing to run the full application.
    */
    public class OnlineDbContextFactory : IDesignTimeDbContextFactory<OnlineDbContext>
    {
        public OnlineDbContext CreateDbContext(string[] args)
        {
            // 1. Build the configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // 2. Get the connection string from appsettings.json
            var connectionString = configuration.GetConnectionString("OnlineConnection");

            // 3. Create the DbContextOptionsBuilder
            var optionsBuilder = new DbContextOptionsBuilder<OnlineDbContext>();
            optionsBuilder.UseSqlServer(connectionString, b =>
            {
                // 4. Tell it which assembly (project) contains the migrations
                b.MigrationsAssembly(typeof(OnlineDbContextFactory).Assembly.FullName);
            });

            // 5. Return the configured DbContext
            return new OnlineDbContext(optionsBuilder.Options);
        }
    }
}