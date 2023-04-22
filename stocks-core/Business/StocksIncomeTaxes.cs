using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_infrastructure.Enums;

namespace stocks_core.Business
{
    public class StocksIncomeTaxes : IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

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
            }
        }

        public void CalculateIncomeTaxesForAllMonths(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {            
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total = 
                AverageTradedPriceCalculator.CalculateAverageTradedPrice(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));
            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            bool sellsSuperiorThan20000 = totalSoldInStocks >= IncomeTaxesConstants.LimitForStocksSelling;

            double totalProfit = total.Select(x => x.Value.Profit).Sum();
            bool dayTraded = InvestorDayTraded(movements);

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && totalProfit > 0) || (dayTraded && totalProfit > 0);

            AssetIncomeTaxes objectToAddIntoResponse = new();

            if (paysIncomeTaxes)
            {
                objectToAddIntoResponse.TotalTaxes = (double)CalculateStocksIncomeTaxes(totalProfit, dayTraded);
            }

            objectToAddIntoResponse.DayTraded = dayTraded;
            objectToAddIntoResponse.TotalProfit = totalProfit;
            objectToAddIntoResponse.TotalSold = totalSoldInStocks;
            objectToAddIntoResponse.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(total));
            objectToAddIntoResponse.AssetTypeId = (int)Assets.Stocks;

            response.Add(objectToAddIntoResponse);
        }

        private static bool InvestorDayTraded(IEnumerable<Movement.EquitMovement> stocksMovements)
        {
            var buys = stocksMovements.Where(x =>
                x.MovementType == B3ServicesConstants.Buy
            );
            var sells = stocksMovements.Where(x =>
                x.MovementType == B3ServicesConstants.Sell
            );

            var dayTradeTransactions = buys.Where(b => sells.Any(s => 
                s.ReferenceDate == b.ReferenceDate && 
                s.TickerSymbol == b.TickerSymbol
            ));

            return dayTradeTransactions.Any();
        }

        private static IEnumerable<(string, string)> DictionaryToList(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total)
        {
            foreach(var asset in total.Values)
            {
                yield return (asset.TickerSymbol, asset.CorporationName);
            }
        }

        private static decimal CalculateStocksIncomeTaxes(double value, bool dayTraded)
        {
            double totalTaxesPorcentage = IncomeTaxesConstants.IncomeTaxesForStocks;

            if (dayTraded)
                totalTaxesPorcentage = IncomeTaxesConstants.IncomeTaxesForDayTrade;

            return ((decimal)totalTaxesPorcentage / 100m) * (decimal)value;
        }
    }
}
