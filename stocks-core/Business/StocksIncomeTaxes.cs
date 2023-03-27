using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Models;
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
            IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            // TO-DO: change operator
            if (totalSoldInStocks > IncomeTaxesConstants.LimitForStocksSelling) return;

            foreach(var movement in stocksMovements)
            {
                // TODO: calculate day-trade.
                // if user day-traded this ticker, pays 20%

                var stockBuys = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Buy)
                );

                var stockSells = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Sell)
                );

                var stockSplits = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Split)
                );

                var stockBonusShares = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.BonusShare)
                );

                var averageTradedPrice = _averageTradedPriceService.CalculateAverageTradedPrice(stockBuys, stockSells, stockSplits, stockBonusShares);
                var currentTickerAverageTradedPrice = _averageTradedPriceRepository.GetAverageTradedPrice(movement.TickerSymbol, accountId)!;

                double profit = averageTradedPrice[movement.TickerSymbol].AverageTradedPrice - currentTickerAverageTradedPrice.AveragePrice;
            }
        }

        public async Task CalculateIncomeTaxesForTheFirstTimeAndSaveAverageTradedPrice(CalculateAssetsIncomeTaxesResponse response, IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));
            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            bool paysIncomeTaxes = false;
            if (totalSoldInStocks >= IncomeTaxesConstants.LimitForStocksSelling) paysIncomeTaxes = true;

            foreach (var movement in stocksMovements)
            {
                // TODO: calculate day-trade.
                // if user day-traded this ticker, pays 20%

                var stockBuys = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Buy)
                );

                var stockSells = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Sell)
                );

                var stockSplits = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Split)
                );

                var stockBonusShares = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.BonusShare)
                );

                var averageTradedPrice = _averageTradedPriceService.CalculateAverageTradedPrice(stockBuys, stockSells, stockSplits, stockBonusShares);
                var currentTickerAverageTradedPrice = _averageTradedPriceRepository.GetAverageTradedPrice(movement.TickerSymbol, accountId);

                // Caso seja a primeira vez que um usuário execute o método, o preço médio pode não estar salvo na base de dados.
                if (currentTickerAverageTradedPrice is null)
                {
                    AverageTradedPrice tickerAverageTradedPrice = new()
                    {
                        Ticker = movement.TickerSymbol,
                        AveragePrice = averageTradedPrice[movement.TickerSymbol].AverageTradedPrice,
                        Quantity = (int)averageTradedPrice[movement.TickerSymbol].CurrentQuantity,
                        AccountId = accountId,
                        UpdatedAt = movement.ReferenceDate,
                    };

                    await _averageTradedPriceRepository.Insert(tickerAverageTradedPrice);
                }

                if (paysIncomeTaxes)
                {
                    // double profit = averageTradedPrice[movement.TickerSymbol].AverageTradedPrice - currentTickerAverageTradedPrice.AveragePrice;
                    double profit = 100;
                    double totalIncomeTaxesToBePaid = (IncomeTaxesConstants.IncomeTaxesForStocks / profit) * 100;

                    response.Assets.Add(new Asset(movement.TickerSymbol, (int)movement.EquitiesQuantity, movement.ReferenceDate, totalIncomeTaxesToBePaid));
                } else
                {
                    response.Assets.Add(new Asset(movement.TickerSymbol, (int)movement.EquitiesQuantity, movement.ReferenceDate, 0));
                }
            }
        }
    }
}
