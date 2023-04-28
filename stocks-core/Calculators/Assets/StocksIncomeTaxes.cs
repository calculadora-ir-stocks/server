using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_infrastructure.Enums;

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

        public void CalculateIncomeTaxesForAllMonths(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> tickersMovementsDetails =
                CalculateAverageTradedPrice(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));
            double totalSold = sells.Sum(stock => stock.OperationValue);

            bool sellsSuperiorThan20000 = totalSold >= IncomeTaxesConstants.LimitForStocksSelling;

            double totalProfit = tickersMovementsDetails.Select(x => x.Value.Profit).Sum();
            bool dayTraded = InvestorDayTraded(movements);

            bool paysIncomeTaxes = sellsSuperiorThan20000 && totalProfit > 0 || dayTraded && totalProfit > 0;

            AssetIncomeTaxes objectToAddIntoResponse = new();

            if (paysIncomeTaxes)
                objectToAddIntoResponse.TotalTaxes = (double)CalculateIncomeTaxes(totalProfit, dayTraded, IncomeTaxesConstants.IncomeTaxesForStocks);

            objectToAddIntoResponse.DayTraded = dayTraded;
            objectToAddIntoResponse.TotalProfitOrLoss = totalProfit;
            objectToAddIntoResponse.TotalSold = totalSold;
            objectToAddIntoResponse.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tickersMovementsDetails));
            objectToAddIntoResponse.AssetTypeId = Assets.Stocks;

            response.Add(objectToAddIntoResponse);
        }
    }
}
