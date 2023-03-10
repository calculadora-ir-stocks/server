using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Business
{
    public class StocksIncomeTaxes : IIncomeTaxesCalculation
    {
        private static double totalSoldInStocks = 0.00;
        private static IAverageTradedPriceRepostory _averageTradedPriceRepository;

        public StocksIncomeTaxes(IAverageTradedPriceRepostory averageTradedPriceRepository)
        {
            _averageTradedPriceRepository = averageTradedPriceRepository;
        }

        /// <summary>
        /// Algoritmo para calcular o imposto de renda a ser pago em ações.
        /// </summary>
        public async Task AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            foreach(var movement in sells) totalSoldInStocks += movement.OperationValue;

            if (totalSoldInStocks > IncomeTaxesConstants.LimitForStocksSelling) return;

            foreach(var movement in movements)
            {
                // TODO: calculate day-trade.
                // if user day-traded this ticker, pays 20%

                var assetBuys = movements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Buy)
                );

                var assetSells = movements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Sell)
                );

                var assetSplits = movements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Split)
                );

                var operationsAverageTradedPrice = AverageTradedPriceService.CalculateAverageTradedPrice(assetBuys, assetSells, assetSplits);
                var averageTradedPrice = _averageTradedPriceRepository.GetAverageTradedPrice(movement.TickerSymbol, accountId);

                var profit = operationsAverageTradedPrice[movement.TickerSymbol].AverageTradedPrice - averageTradedPrice.AveragePrice;
                var incomeTaxesToBePaid = ((double) profit / 100) * IncomeTaxesConstants.IncomeTaxesForStocks;
            }
        }
    }
}
