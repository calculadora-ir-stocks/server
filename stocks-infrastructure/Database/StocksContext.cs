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
        public DbSet<Asset> Assets { get; } = null!;

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

            modelBuilder.Entity<Account>()
                .HasMany(ap => ap.IncomeTaxes)
                .WithOne(ap => ap.Account);

            modelBuilder.Entity<Account>()
                .HasMany(ap => ap.AverageTradedPrices)
                .WithOne(ap => ap.Account);

            modelBuilder.Entity<IncomeTaxes>()
                .HasOne(ap => ap.Account)
                .WithMany(ap => ap.IncomeTaxes);

            modelBuilder.Entity<AverageTradedPrice>()
                .HasOne(ap => ap.Account)
                .WithMany(ap => ap.AverageTradedPrices);

            modelBuilder.Entity<Asset>().HasData
            (
                new Asset(1, "Ações"),
                new Asset(2, "ETF - Exchange Traded Fund"),
                new Asset(3, "FII - Fundo de Investimento Imobiliário"),
                new Asset(4, "Fundos de Investimentos"),
                new Asset(5, "BDR - Brazilian Depositary Receipts"),
                new Asset(6, "Ouro")
            );
        }
    }
}
