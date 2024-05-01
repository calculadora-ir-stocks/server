using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Api.Database
{
    public class StocksContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Asset> Assets { get; } = null!;
        public DbSet<AverageTradedPrice> AverageTradedPrices { get; set; } = null!;
        public DbSet<IncomeTaxes> IncomeTaxes { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<Infrastructure.Models.Audit> Audits { get; set; } = null!;

        private readonly ILogger<StocksContext> logger;

        public StocksContext(DbContextOptions<StocksContext> options, ILogger<StocksContext> logger) : base(options)
        {
            this.logger = logger;

            try
            {
                Database.EnsureCreated();
            } catch (Exception e)
            {
                this.logger.LogError(e, "Ocorreu um erro ao carregar o banco de dados.");
            }
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

            modelBuilder.Entity<Account>()
                .HasOne(ap => ap.Plan)
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
