using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Business
{
    public class StocksIncomeTaxes : IIncomeTaxesCalculator
    {
        private readonly IAverageTradedPriceRepostory _averageTradedPriceRepository;
        private readonly IAverageTradedPriceService _averageTradedPriceService;

        public StocksIncomeTaxes(IAverageTradedPriceRepostory averageTradedPriceRepository, IAverageTradedPriceService averageTradedPriceService)
        {
            _averageTradedPriceRepository = averageTradedPriceRepository;
            _averageTradedPriceService = averageTradedPriceService;
        }

        /// <summary>
        /// Algoritmo para calcular o imposto de renda a ser pago em ações.
        /// </summary>
        public async Task AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

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

                var bonusShares = movements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.BonusShare)
                );

                var averageTradedPrice = _averageTradedPriceService.CalculateAverageTradedPrice(assetBuys, assetSells, assetSplits, bonusShares);
                var currentTickerAverageTradedPrice = _averageTradedPriceRepository.GetAverageTradedPrice(movement.TickerSymbol, accountId);

                double profit = averageTradedPrice[movement.TickerSymbol].AverageTradedPrice - currentTickerAverageTradedPrice.AveragePrice;
            }
        }
    }
}
