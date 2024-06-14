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
        public DbSet<BonusShare> BonusShares { get; set; } = null!;
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
            }
            catch (Exception e)
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

            modelBuilder.Entity<BonusShare>().HasData
            (
                new BonusShare("KLBN11", new DateTime(2024, 5, 6).Date, 14.24, 0.10f),
                new BonusShare("KLBN3", new DateTime(2024, 5, 6).Date, 2.85, 0.10f),
                new BonusShare("KLBN4", new DateTime(2024, 5, 6).Date, 2.85, 0.10f),
                new BonusShare("CMIG3", new DateTime(2024, 4, 29).Date, 5.00, 0.30f),
                new BonusShare("CMIG4", new DateTime(2024, 4, 29).Date, 5.00, 0.30f),
                new BonusShare("ALUP11", new DateTime(2024, 4, 19).Date, 29.76, 0.04f),
                new BonusShare("ALUP3", new DateTime(2024, 4, 19).Date, 9.92, 0.04f),
                new BonusShare("ALUP4", new DateTime(2024, 4, 19).Date, 9.92, 0.04f),
                new BonusShare("UNIP3", new DateTime(2024, 4, 18).Date, 23.32, 0.10f),
                new BonusShare("UNIP5", new DateTime(2024, 4, 18).Date, 23.32, 0.10f),
                new BonusShare("UNIP6", new DateTime(2024, 4, 18).Date, 23.32, 0.10f),
                new BonusShare("GGBR3", new DateTime(2024, 4, 17).Date, 11.55, 0.20f),
                new BonusShare("GGBR4", new DateTime(2024, 4, 17).Date, 11.55, 0.20f),
                new BonusShare("VITT3", new DateTime(2024, 4, 12).Date, 0.00, 0.10f),
                new BonusShare("ROMI3", new DateTime(2024, 4, 1).Date, 18.86, 0.05f),
                new BonusShare("POMO3", new DateTime(2024, 3, 7).Date, 5.28, 0.20f),
                new BonusShare("POMO4", new DateTime(2024, 3, 7).Date, 5.28, 0.20f),
                new BonusShare("ITSA3", new DateTime(2023, 11, 27).Date, 17.92, 0.05f),
                new BonusShare("ITSA4", new DateTime(2023, 11, 27).Date, 17.92, 0.05f),
                new BonusShare("FLRY3", new DateTime(2023, 6, 12).Date, 6.52, 0.05f),
                new BonusShare("RADL3", new DateTime(2023, 5, 19).Date, 22.70, 0.04f),
                new BonusShare("SLCE3", new DateTime(2023, 5, 8).Date, 23.54, 0.10f),
                new BonusShare("LUXM3", new DateTime(2023, 4, 27).Date, 0.96, 3.00f),
                new BonusShare("LUXM4", new DateTime(2023, 4, 27).Date, 0.96, 3.00f),
                new BonusShare("ALUP11", new DateTime(2023, 4, 17).Date, 28.05, 0.04f),
                new BonusShare("ALUP3", new DateTime(2023, 4, 17).Date, 9.35, 0.04f),
                new BonusShare("ALUP4", new DateTime(2023, 4, 17).Date, 9.35, 0.04f),
                new BonusShare("ROMI3", new DateTime(2023, 4, 3).Date, 16.53, 0.10f),
                new BonusShare("GGBR3", new DateTime(2023, 3, 21).Date, 11.55, 0.05f),
                new BonusShare("GGBR4", new DateTime(2023, 3, 21).Date, 11.55, 0.05f),
                new BonusShare("DXCO3", new DateTime(2022, 12, 20).Date, 13.14, 0.10f),
                new BonusShare("ITSA3", new DateTime(2022, 11, 10).Date, 13.65, 0.10f),
                new BonusShare("ITSA4", new DateTime(2022, 11, 10).Date, 13.65, 0.10f),
                new BonusShare("ATOM3", new DateTime(2022, 4, 29), 3.04, 0.14f),
                new BonusShare("ROMI3", new DateTime(2022, 9, 30), 18.23, 0.10f),
                new BonusShare("CMIG3", new DateTime(2022, 4, 29), 5.00, 0.30f),
                new BonusShare("CMIG4", new DateTime(2022, 4, 29), 5.00, 0.30f),
                new BonusShare("UNIP3", new DateTime(2022, 4, 20), 24.23, 0.10f),
                new BonusShare("UNIP5", new DateTime(2022, 4, 20), 24.23, 0.10f),
                new BonusShare("UNIP6", new DateTime(2022, 4, 20), 24.23, 0.10f),
                new BonusShare("BBDC3", new DateTime(2022, 4, 18), 4.13, 0.10f),
                new BonusShare("BBDC4", new DateTime(2022, 4, 18), 4.13, 0.10f),
                new BonusShare("SHUL4", new DateTime(2022, 4, 13), 0.90, 1.00f),
                new BonusShare("SHUL3", new DateTime(2022, 4, 13), 0.90, 1.00f),
                new BonusShare("SLCE3", new DateTime(2021, 12, 30), 25.89, 0.10f),
                new BonusShare("ITSA3", new DateTime(2021, 12, 20), 18.89, 0.05f),
                new BonusShare("ITSA4", new DateTime(2021, 12, 20), 18.89, 0.05f),
                new BonusShare("DXCO3", new DateTime(2021, 12, 14), 5.78, 0.10f),
                new BonusShare("LREN3", new DateTime(2021, 11, 4), 13.35, 0.10f),
                new BonusShare("PSSA3", new DateTime(2021, 10, 20), 12.37, 1.00f),
                new BonusShare("BRAP3", new DateTime(2021, 9, 20), 36.84, 0.1295f),
                new BonusShare("BRAP4", new DateTime(2021, 9, 20), 36.84, 0.1295f),
                new BonusShare("CMIG3", new DateTime(2021, 4, 30), 5.00, 0.1150f),
                new BonusShare("CMIG4", new DateTime(2021, 4, 30), 5.00, 0.1150f),
                new BonusShare("BBDC3", new DateTime(2021, 4, 16), 4.53, 0.10f),
                new BonusShare("BBDC4", new DateTime(2021, 4, 16), 4.53, 0.10f),
                new BonusShare("SHUL4", new DateTime(2021, 4, 15), 0.26, 1.00f),
                new BonusShare("SHUL3", new DateTime(2021, 4, 15), 0.26, 1.00f),
                new BonusShare("SULA11", new DateTime(2021, 3, 29), 41.19, 0.0605f),
                new BonusShare("SULA3", new DateTime(2021, 3, 29), 13.73, 0.0605f),
                new BonusShare("SULA4", new DateTime(2021, 3, 29), 13.73, 0.0605f),
                new BonusShare("SULA11", new DateTime(2020, 11, 26), 40.47, 0.0188f),
                new BonusShare("SULA3", new DateTime(2020, 11, 26), 13.49, 0.0188f),
                new BonusShare("SULA4", new DateTime(2020, 11, 26), 13.49, 0.0188f),
                new BonusShare("DOHL3", new DateTime(2020, 7, 29), 2.97, 0.25f),
                new BonusShare("DOHL4", new DateTime(2020, 7, 29), 2.97, 0.25f),
                new BonusShare("ROMI3", new DateTime(2020, 10, 28), 13.91, 0.1667f),
                new BonusShare("CMIG3", new DateTime(2020, 7, 31), 5.00, 0.0411f),
                new BonusShare("CMIG4", new DateTime(2020, 7, 31), 5.00, 0.0411f),
                new BonusShare("CTSA3", new DateTime(2020, 7, 1), 0.42, 1.8321f),
                new BonusShare("CTSA4", new DateTime(2020, 7, 1), 0.42, 1.8321f),
                new BonusShare("BBDC3", new DateTime(2020, 4, 13), 4.96, 0.10f),
                new BonusShare("BBDC4", new DateTime(2020, 4, 13), 4.96, 0.10f),
                new BonusShare("RENT3", new DateTime(2019, 12, 20), 18.78, 0.05f)
            );
        }
    }
}
