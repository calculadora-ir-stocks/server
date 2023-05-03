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

        public void CalculateIncomeTaxesForAllMonths(List<AssetIncomeTaxes> response, string month, IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> tickersMovements =
                CalculateAverageTradedPrice(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell) && x.ReferenceDate.ToString("MM") == month);

            double totalSold = sells.Sum(stock => stock.OperationValue);
            bool sellsSuperiorThan20000 = totalSold >= IncomeTaxesConstants.LimitForStocksSelling;

            double swingTradeProfit = tickersMovements.Where(x => !x.Value.DayTraded && x.Value.Month == month).Select(x => x.Value.Profit).Sum();
            double dayTradeProfit = tickersMovements.Where(x => x.Value.DayTraded && x.Value.Month == month).Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && swingTradeProfit > 0) || (dayTradeProfit > 0);

            AssetIncomeTaxes objectToAddIntoResponse = new();

            if (paysIncomeTaxes)
                objectToAddIntoResponse.TotalTaxes = (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, IncomeTaxesConstants.IncomeTaxesForStocks);

            bool dayTraded = tickersMovements.Where(x => x.Value.DayTraded && x.Value.Month == month).Any();
            objectToAddIntoResponse.DayTraded = dayTraded;

            objectToAddIntoResponse.SwingTradeProfit = swingTradeProfit;
            objectToAddIntoResponse.DayTradeProfit = dayTradeProfit;
            objectToAddIntoResponse.TotalSold = totalSold;
            objectToAddIntoResponse.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tickersMovements));
            objectToAddIntoResponse.AssetTypeId = stocks_infrastructure.Enums.Assets.Stocks;

            response.Add(objectToAddIntoResponse);
        }
    }
}
