using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Calculators.Assets
{
    public class StocksIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            if (totalSoldInStocks > IncomeTaxesConstants.LimitForStocksSelling) return;

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
            }
        }

        public void CalculateIncomeTaxesForAllMonths(AssetIncomeTaxes response, string month, IEnumerable<Movement.EquitMovement> movements)
        {
            movements = movements.Where(x => x.ReferenceDate.ToString("MM") == month).ToList();

            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> tickersMovements =
                CalculateAverageTradedPrice(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSold = sells.Sum(stock => stock.OperationValue);
            bool sellsSuperiorThan20000 = totalSold >= IncomeTaxesConstants.LimitForStocksSelling;

            double swingTradeProfit = tickersMovements.Where(x => !x.Value.DayTraded).Select(x => x.Value.Profit).Sum();
            double dayTradeProfit = tickersMovements.Where(x => x.Value.DayTraded).Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && swingTradeProfit > 0) || (dayTradeProfit > 0);

            if (paysIncomeTaxes)
                response.Taxes = (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, IncomeTaxesConstants.IncomeTaxesForStocks);

            bool dayTraded = tickersMovements.Where(x => x.Value.DayTraded).Any();
            response.DayTraded = dayTraded;

            response.SwingTradeProfit = swingTradeProfit;
            response.DayTradeProfit = dayTradeProfit;
            response.TotalSold = totalSold;
            response.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tickersMovements));
            response.AssetTypeId = stocks_infrastructure.Enums.Assets.Stocks;
        }
    }
}
