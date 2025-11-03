using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgriPosPoC.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriPosPoC.Core.Data
{
    public class OnlineDbContext : DbContext
    {
        public OnlineDbContext(DbContextOptions<OnlineDbContext> options) : base(options) { }
        public DbSet<Invoice> Invoices { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // This tells EF Core to create the 'Amount' column as decimal(18, 2)
            modelBuilder.Entity<Invoice>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }
}