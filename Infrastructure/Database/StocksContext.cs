using Microsoft.EntityFrameworkCore;
using Common.Constants;
using Infrastructure.Models;
using System.Reflection;

namespace Api.Database
{
    public class StocksContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Asset> Assets { get; } = null!;
        public DbSet<AverageTradedPrice> AverageTradedPrices { get; set; } = null!;
        public DbSet<IncomeTaxes> IncomeTaxes { get; set; } = null!;
        public DbSet<EmailCode> EmailCodes { get; set; } = null!;
        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<PremiumCode> PremiumCodes { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;

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

            modelBuilder.Entity<Account>()
                .HasOne(ap => ap.Plan);

            modelBuilder.Entity<IncomeTaxes>()
                .HasOne(ap => ap.Account)
                .WithMany(ap => ap.IncomeTaxes);

            modelBuilder.Entity<EmailCode>()
                .HasOne(ap => ap.Account)
                .WithOne(ap => ap.EmailCode);

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

            modelBuilder.Entity<Plan>().HasData
            (
                new Plan(PlansConstants.Free, "Gratuito", "Plano gratuito", "price_1NkZ3XElcTcz6jitSpsX5bce"),
                new Plan(PlansConstants.Monthly, "Mensal", "R$39,99 por mês", "price_1NioETElcTcz6jitFPhhg4HH"),
                new Plan(PlansConstants.Semester, "Semestral", "R$29,99 por mês", "price_1Ninm4ElcTcz6jitB40ifv1V"),
                new Plan(PlansConstants.Anual, "Anual", "R$19,99 por mês", "price_1NiniyElcTcz6jit3AKGk57n")
            );

            modelBuilder.Entity<PremiumCode>().HasData
            (
                // TODO
                // Based on the pre-launch waitlist, generate X premium codes.
                new PremiumCode()
            );
        }
    }
}
