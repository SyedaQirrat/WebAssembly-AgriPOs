// /Data/OfflineDbContext.cs

using AgriPosPoC.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriPosPoC.Core.Data
{
    public class OfflineDbContext : DbContext
    {
        public OfflineDbContext(DbContextOptions<OfflineDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Product> Products { get; set; } // Add Product here
    }
}