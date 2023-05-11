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
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMonth(AssetIncomeTaxes response, IEnumerable<Movement.EquitMovement> movements)
        {
            var tradedTickersDetails = CalculateMovements(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSold = sells.Sum(stock => stock.OperationValue);
            bool sellsSuperiorThan20000 = totalSold >= IncomeTaxesConstants.LimitForStocksSelling;

            double swingTradeProfit = tradedTickersDetails.Where(x => !x.Value.DayTraded).Select(x => x.Value.Profit).Sum();
            double dayTradeProfit = tradedTickersDetails.Where(x => x.Value.DayTraded).Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && swingTradeProfit > 0) || (dayTradeProfit > 0);

            if (paysIncomeTaxes)
                response.Taxes = (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, IncomeTaxesConstants.IncomeTaxesForStocks);

            bool dayTraded = tradedTickersDetails.Where(x => x.Value.DayTraded).Any();
            response.DayTraded = dayTraded;

            response.SwingTradeProfit = swingTradeProfit;
            response.DayTradeProfit = dayTradeProfit;
            response.TotalSold = totalSold;
            response.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tradedTickersDetails));
            response.AssetTypeId = stocks_infrastructure.Enums.Assets.Stocks;
        }

        public List<TickerAverageTradedPrice> GetTickersAverageTradedPrice()
        {
            return GetListContainingAverageTradedPrices();
        }
    }
}
