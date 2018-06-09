using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace GoldenCrosser.Database
{
    class InvestingContext : DbContext
    {
        public DbSet<Investment> Investments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Investment>()
                .HasRequired(i => i.Company);
        }
    }
}
