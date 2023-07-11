using Microsoft.IdentityModel.Tokens;
using stocks_common.Exceptions;
using stocks_common.Models;
using stocks_core.Calculators;
using stocks_core.Calculators.Assets;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;
using stocks_infrastructure.Dtos;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Services.BigBang
{
    public class BigBang : IBigBang
    {
        private IIncomeTaxesCalculator calculator;
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

        public BigBang(IIncomeTaxesCalculator calculator, IAverageTradedPriceRepostory averageTradedPriceRepository)
        {
            this.calculator = calculator;
            this.averageTradedPriceRepository = averageTradedPriceRepository;
        }

        public async Task<(List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>)> Execute(Movement.Root? request, Guid accountId)
        {
            var movements = GetInvestorMovements(request);
            if (movements.IsNullOrEmpty()) throw new NoneMovementsException("O usuário não possui nenhuma movimentação na bolsa até então.");

            movements = OrderMovementsByDateAndMovementType(movements);

            Dictionary<string, List<Movement.EquitMovement>> monthlyMovements = new();
            var monthsThatHadMovements = movements.Select(x => x.ReferenceDate.ToString("MM/yyyy")).Distinct();

            foreach (var month in monthsThatHadMovements)
            {
                var monthMovements = movements.Where(x => x.ReferenceDate.ToString("MM/yyyy") == month).ToList();
                monthlyMovements.Add(month, monthMovements);
            }

            return await GetTaxesAndAverageTradedPrices(monthlyMovements, accountId);
        }

        private static List<Movement.EquitMovement> GetInvestorMovements(Movement.Root? response)
        {
            if (response is null || response.Data is null) return new List<Movement.EquitMovement>();

            var movements = response.Data.EquitiesPeriods.EquitiesMovements;

            return movements
                .Where(x =>
                    x.MovementType.Equals(B3ResponseConstants.Buy) ||
                    x.MovementType.Equals(B3ResponseConstants.Sell) ||
                    x.MovementType.Equals(B3ResponseConstants.Split) ||
                    x.MovementType.Equals(B3ResponseConstants.ReverseSplit) ||
                    x.MovementType.Equals(B3ResponseConstants.BonusShare))
                .ToList();
        }

        /// <summary>
        /// Ordena as operações por ordem crescente através da data - a B3 retorna em ordem decrescente - e
        /// ordena operações de compra antes das operações de venda em operações day trade.
        /// </summary>
        private List<Movement.EquitMovement> OrderMovementsByDateAndMovementType(IList<Movement.EquitMovement> movements)
        {
            // TODO: a premissa da descrição do método está correta?
            return movements.OrderBy(x => x.MovementType).OrderBy(x => x.ReferenceDate).ToList();
        }

        private async Task<(List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>)> GetTaxesAndAverageTradedPrices(
            Dictionary<string, List<Movement.EquitMovement>> monthlyMovements, Guid accountId)
        {
            List<AssetIncomeTaxes> assetsIncomeTaxes = new();
            List<AverageTradedPriceDetails> averageTradedPrices = new();

            foreach (var monthMovements in monthlyMovements)
            {
                SetDayTradeOperations(monthMovements.Value);

                var stocks = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.Stocks));
                var etfs = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.ETFs));
                var fiis = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.FIIs));
                var bdrs = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.BDRs));
                var gold = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.Gold));
                var fundInvestments = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.InvestmentsFunds));

                if (stocks.Any())
                {
                    var prices = await GetAverageTradedPrices(accountId, stocks);
                    averageTradedPrices.AddRange(ToAverageTradedPriceDetails(prices, stocks_common.Enums.Asset.Stocks));

                    calculator = new StocksIncomeTaxes(); 
                    calculator.CalculateIncomeTaxes(assetsIncomeTaxes, averageTradedPrices, stocks, monthMovements.Key);
                }

                if (etfs.Any())
                {
                    var prices = await GetAverageTradedPrices(accountId, etfs);
                    averageTradedPrices.AddRange(ToAverageTradedPriceDetails(prices, stocks_common.Enums.Asset.ETFs));

                    calculator = new ETFsIncomeTaxes();
                    calculator.CalculateIncomeTaxes(assetsIncomeTaxes, averageTradedPrices, etfs, monthMovements.Key);
                }

                if (fiis.Any())
                {
                    var prices = await GetAverageTradedPrices(accountId, fiis);
                    averageTradedPrices.AddRange(ToAverageTradedPriceDetails(prices, stocks_common.Enums.Asset.FIIs));

                    calculator = new FIIsIncomeTaxes();
                    calculator.CalculateIncomeTaxes(assetsIncomeTaxes, averageTradedPrices, fiis, monthMovements.Key);
                }

                if (bdrs.Any())
                {
                    var prices = await GetAverageTradedPrices(accountId, bdrs);
                    averageTradedPrices.AddRange(ToAverageTradedPriceDetails(prices, stocks_common.Enums.Asset.BDRs));

                    calculator = new BDRsIncomeTaxes();
                    calculator.CalculateIncomeTaxes(assetsIncomeTaxes, averageTradedPrices, bdrs, monthMovements.Key);
                }

                if (gold.Any())
                {
                    var prices = await GetAverageTradedPrices(accountId, gold);
                    averageTradedPrices.AddRange(ToAverageTradedPriceDetails(prices, stocks_common.Enums.Asset.Gold));

                    calculator = new GoldIncomeTaxes();
                    calculator.CalculateIncomeTaxes(assetsIncomeTaxes, averageTradedPrices, gold, monthMovements.Key);
                }

                if (fundInvestments.Any())
                {
                    var prices = await GetAverageTradedPrices(accountId, fundInvestments);
                    averageTradedPrices.AddRange(ToAverageTradedPriceDetails(prices, stocks_common.Enums.Asset.InvestmentsFunds));

                    calculator = new InvestmentsFundsIncomeTaxes();
                    calculator.CalculateIncomeTaxes(assetsIncomeTaxes, averageTradedPrices, fundInvestments, monthMovements.Key);
                }
            }

            return (assetsIncomeTaxes, averageTradedPrices);
        }

        private IEnumerable<AverageTradedPriceDetails> ToAverageTradedPriceDetails(IEnumerable<AverageTradedPriceDto> prices, stocks_common.Enums.Asset assetType)
        {
            foreach (var price in prices)
            {
                yield return new AverageTradedPriceDetails(
                    tickerSymbol: price.Ticker,
                    averageTradedPrice: price.AverageTradedPrice,
                    totalBought: price.AverageTradedPrice,
                    tradedQuantity: price.Quantity,
                    assetType
                );
            }
        }

        private async Task <IEnumerable<AverageTradedPriceDto>> GetAverageTradedPrices(Guid accountId, IEnumerable<Movement.EquitMovement> movements)
        {
            return await averageTradedPriceRepository.GetAverageTradedPrices(accountId, movements.Select(x => x.TickerSymbol).ToList());
        }

        /// <summary>
        /// Altera a propriedade booleana DayTraded para verdadeiro em operações de venda day-trade.
        /// </summary>
        private static void SetDayTradeOperations(List<Movement.EquitMovement> movements)
        {
            var buys = movements.Where(x => x.MovementType == B3ResponseConstants.Buy);
            var sells = movements.Where(x => x.MovementType == B3ResponseConstants.Sell);

            var dayTradeSellsOperationsIds = sells.Where(b => buys.Any(s =>
                s.ReferenceDate == b.ReferenceDate &&
                s.TickerSymbol == b.TickerSymbol
            )).Select(x => x.Id);

            foreach(var id in dayTradeSellsOperationsIds)
            {
                var dayTradeOperation = movements.Where(x => x.Id == id).Single();
                dayTradeOperation.DayTraded = true;
            }
        }
    }
}
