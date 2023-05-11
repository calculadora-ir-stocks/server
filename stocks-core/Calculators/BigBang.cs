using Microsoft.IdentityModel.Tokens;
using stocks.Models;
using stocks.Repositories;
using stocks_common.Models;
using stocks_core.Calculators.Assets;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_infrastructure.Repositories.IncomeTaxes;

namespace stocks_core.Business
{
    /// <summary>
    /// Classe responsável por calcular o imposto de renda a ser pago em todos os ativos de 01/11/2019 até D-1 e de salvar
    /// na base de dados o preço médio de cada ativo.
    /// </summary>
    public class BigBang
    {
        private readonly IIncomeTaxesRepository incomeTaxesRepository;
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;
        private readonly IGenericRepository<Account> genericRepositoryAccount;

        public BigBang(IIncomeTaxesRepository incomeTaxesRepository, IAverageTradedPriceRepostory averageTradedPriceRepository,
            IGenericRepository<Account> genericRepositoryAccount)
        {
            this.incomeTaxesRepository = incomeTaxesRepository;
            this.averageTradedPriceRepository = averageTradedPriceRepository;
            this.genericRepositoryAccount = genericRepositoryAccount;
        }

        public async Task Calculate(Movement.Root response, IIncomeTaxesCalculator calculator, Guid accountId)
        {
            var movements = GetAllMovements(response);
            if (movements.IsNullOrEmpty()) return;

            OrderMovementsByDateAndMovementType(movements);

            var monthlyMovements = new Dictionary<string, List<Movement.EquitMovement>>();
            var monthsThatHadMovements = movements.Select(x => x.ReferenceDate.ToString("MM")).Distinct();

            foreach (var month in monthsThatHadMovements)
            {
                monthlyMovements.Add(month, movements.Where(x => x.ReferenceDate.ToString("MM") == month).ToList());
            }

            await CalculateTaxesForEachMonth(monthlyMovements, calculator, accountId);
        }

        private static List<Movement.EquitMovement> GetAllMovements(Movement.Root response)
        {
            var movements = response.Data.EquitiesPeriods.EquitiesMovements;

            return movements
                .Where(x =>
                    x.MovementType.Equals(B3ServicesConstants.Buy) ||
                    x.MovementType.Equals(B3ServicesConstants.Sell) ||
                    x.MovementType.Equals(B3ServicesConstants.Split) ||
                    x.MovementType.Equals(B3ServicesConstants.ReverseSplit) ||
                    x.MovementType.Equals(B3ServicesConstants.BonusShare)).ToList();
        }

        /// <summary>
        /// Ordena as operações por ordem crescente através da data - a B3 retorna em ordem decrescente - e
        /// ordena operações de compra antes das operações de venda em operações day trade.
        /// </summary>
        private static void OrderMovementsByDateAndMovementType(IList<Movement.EquitMovement> movements)
        {
            movements = movements.OrderBy(x => x.MovementType).OrderBy(x => x.ReferenceDate).ToList();
        }

        private async Task CalculateTaxesForEachMonth(Dictionary<string, List<Movement.EquitMovement>> monthlyMovements, IIncomeTaxesCalculator calculator, Guid accountId)
        {
            Dictionary<string, AssetIncomeTaxes> assetsIncomeTaxes = new();

            foreach (var monthMovements in monthlyMovements)
            {
                assetsIncomeTaxes.Add(monthMovements.Key, new AssetIncomeTaxes());

                var stocks = monthMovements.Value.Where(x => x.AssetType.Equals(AssetMovementTypes.Stocks));
                var etfs = monthMovements.Value.Where(x => x.AssetType.Equals(AssetMovementTypes.ETFs));
                var fiis = monthMovements.Value.Where(x => x.AssetType.Equals(AssetMovementTypes.FIIs));
                var bdrs = monthMovements.Value.Where(x => x.AssetType.Equals(AssetMovementTypes.BDRs));
                var gold = monthMovements.Value.Where(x => x.AssetType.Equals(AssetMovementTypes.Gold));
                var fundInvestments = monthMovements.Value.Where(x => x.AssetType.Equals(AssetMovementTypes.FundInvestments));

                if (stocks.Any())
                {
                    calculator = new StocksIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMonth(assetsIncomeTaxes[monthMovements.Key], stocks);
                }

                if (etfs.Any())
                {
                    calculator = new ETFsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMonth(assetsIncomeTaxes[monthMovements.Key], etfs);
                }

                if (fiis.Any())
                {
                    calculator = new FIIsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMonth(assetsIncomeTaxes[monthMovements.Key], fiis);
                }

                if (bdrs.Any())
                {
                    calculator = new BDRsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMonth(assetsIncomeTaxes[monthMovements.Key], bdrs);
                }

                if (gold.Any())
                {
                    calculator = new GoldIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMonth(assetsIncomeTaxes[monthMovements.Key], gold);
                }

                if (fundInvestments.Any())
                {
                    calculator = new InvestmentsFundsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMonth(assetsIncomeTaxes[monthMovements.Key], fundInvestments);
                }
            }

            await SaveIntoDatabase(assetsIncomeTaxes, calculator.GetTickersAverageTradedPrice(), accountId);
        }

        private async Task SaveIntoDatabase(Dictionary<string, AssetIncomeTaxes> response,
            List<TickerAverageTradedPrice> assetsAverageTradedPrices, Guid accountId)
        {
            Account account = genericRepositoryAccount.GetById(accountId);

            // The loneliness of building a software company by my own is making me sad. If you're reading this, wanna partner up?
            List<IncomeTaxes> incomeTaxes = new();
            List<AverageTradedPrice> averageTradedPrices = new();

            foreach(var movement in response)
            {
                incomeTaxes.Add(new IncomeTaxes
                {
                    Month = movement.Key,
                    TotalTaxes = movement.Value.Taxes,
                    TotalSold = movement.Value.TotalSold,
                    SwingTradeProfit = movement.Value.SwingTradeProfit,
                    DayTradeProfit = movement.Value.DayTradeProfit,
                    TradedAssets = movement.Value.TradedAssets,
                    DayTraded = movement.Value.DayTraded,
                    Account = account,
                    AssetId = (int)movement.Value.AssetTypeId
                });
            }

            foreach (var asset in assetsAverageTradedPrices)
            {
                averageTradedPrices.Add(new AverageTradedPrice
                {
                    Account = account,
                    Ticker = asset.TickerSymbol,
                    AveragePrice = asset.AverageTradedPrice,
                    Quantity = asset.TradedQuantity,
                    UpdatedAt = DateTime.Now
                });
            }

            await incomeTaxesRepository.AddAllAsync(incomeTaxes);
            await averageTradedPriceRepository.AddAllAsync(averageTradedPrices);
        }
    }
}
