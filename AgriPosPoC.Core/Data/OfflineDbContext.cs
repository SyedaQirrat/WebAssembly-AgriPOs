using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgriPosPoC.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriPosPoC.Core.Data
{
    public class OfflineDbContext : DbContext
    {
        public OfflineDbContext(DbContextOptions<OfflineDbContext> options) : base(options) { }
        public DbSet<Invoice> Invoices { get; set; }
    }
}