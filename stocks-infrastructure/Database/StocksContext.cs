using Microsoft.EntityFrameworkCore;
using stocks.Models;
using stocks_infrastructure.Models;
using System.Reflection;

namespace stocks.Database
{
    public class StocksContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<AverageTradedPrice> AverageTradedPrices { get; set; } = null!;
        public DbSet<IncomeTaxes> IncomeTaxes { get; set; } = null!;

        public StocksContext()
        {
            Database.EnsureCreated();
        }

        public StocksContext(DbContextOptions<StocksContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.HasPostgresExtension("uuid-ossp");
        }

        private static void ModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<BaseEntity>();

            modelBuilder.Entity<AverageTradedPrice>()
                .HasOne(ap => ap.Account)
                .WithMany(ap => ap.AverageTradedPrices)
                .HasForeignKey(ap => ap.AccountId);

            modelBuilder.Entity<IncomeTaxes>()
                .HasOne(ap => ap.Account)
                .WithMany(ap => ap.IncomeTaxes)
                .HasForeignKey(ap => ap.AccountId);
        }
    }
}
