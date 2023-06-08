using Newtonsoft.Json;
using stocks_common.Enums;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators.Assets
{
    public class StocksIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response, 
            IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMovements(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            var (dayTradeOperations, swingTradeOperations) = CalculateProfit(movements);

            var dayTradeProfit = dayTradeOperations.Values.Select(x => x.Profit).Sum();
            var swingTradeProfit = swingTradeOperations.Values.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));
            double totalSold = sells.Sum(stock => stock.OperationValue);

            bool sellsSuperiorThan20000 = totalSold >= AliquotConstants.LimitForStocksSelling;

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && swingTradeProfit > 0) || (dayTradeProfit > 0);

            response.Add(new AssetIncomeTaxes
            {
                AssetTypeId = Asset.Stocks,
                Taxes = paysIncomeTaxes ? (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForStocks) : 0,
                TotalSold = totalSold,
                AverageTradedPrices = GetAverageTradedPrice(Asset.Stocks).ToList(),
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit,
                TradedAssets = JsonConvert.SerializeObject(ToDto(movements, B3ResponseConstants.Stocks)),
            });
        }
    }
}
