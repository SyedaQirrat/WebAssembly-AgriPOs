// /Data/OnlineDbContext.cs

using AgriPosPoC.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriPosPoC.Core.Data
{
    public class OnlineDbContext : DbContext
    {
        public OnlineDbContext(DbContextOptions<OnlineDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Product> Products { get; set; } // Add Product here
    }
}