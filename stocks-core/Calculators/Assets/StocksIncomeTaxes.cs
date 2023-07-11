using Newtonsoft.Json;
using stocks_common.Enums;
using stocks_common.Helpers;
using stocks_common.Models;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators.Assets
{
    public class StocksIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateIncomeTaxes(
            List<AssetIncomeTaxes> response,
            List<AverageTradedPriceDetails> averageTradedPrices,
            IEnumerable<Movement.EquitMovement> movements,
            string month
        )
        {
            var (dayTradeOperations, swingTradeOperations) = CalculateProfit(movements, averageTradedPrices);

            var dayTradeProfit = dayTradeOperations.Select(x => x.Profit).Sum();
            var swingTradeProfit = swingTradeOperations.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));
            double totalSold = sells.Sum(stock => stock.OperationValue);

            bool sellsSuperiorThan20000 = totalSold >= AliquotConstants.LimitForStocksSelling;

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && swingTradeProfit > 0) || (dayTradeProfit > 0);
            double taxes = paysIncomeTaxes ? (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForStocks) : 0;

            response.Add(new AssetIncomeTaxes(month, AssetTypeHelper.GetNameByAssetType(Asset.Stocks))
            {
                AssetTypeId = Asset.Stocks,
                Taxes = taxes,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit,
                TradedAssets = JsonConvert.SerializeObject(ConcatOperations(dayTradeOperations, swingTradeOperations))
            });

            AddIntoAverageTradedPricesList(averageTradedPrices, Asset.Stocks);
        }
    }
}
