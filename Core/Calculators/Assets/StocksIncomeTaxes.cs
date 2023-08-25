using Common.Enums;
using Common.Helpers;
using Common.Models;
using Core.Constants;
using Core.DTOs.B3;
using Core.Models;

namespace Core.Calculators.Assets
{
    public class StocksIncomeTaxes : ProfitCalculator, IIncomeTaxesCalculator
    {
        public void Execute(
            List<AssetIncomeTaxes> assetsIncomeTaxes,
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

            assetsIncomeTaxes.Add(new AssetIncomeTaxes
            (
                month, AssetTypeHelper.GetNameByAssetType(Asset.Stocks), GetOperationDetails()
            )
            {
                AssetTypeId = Asset.Stocks,
                Taxes = paysIncomeTaxes ? (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForStocks) : 0,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit
            });
        }
    }
}
