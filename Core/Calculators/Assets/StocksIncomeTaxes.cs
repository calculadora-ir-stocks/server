using Common.Enums;
using Common.Helpers;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace Core.Calculators.Assets
{
    public class StocksIncomeTaxes : ProfitCalculator, IIncomeTaxesCalculator
    {
        public void Execute(
            InvestorMovementDetails investorMovementDetails,
            IEnumerable<Movement.EquitMovement> movements,
            string month
        )
        {
            var profit = CalculateProfit(movements, investorMovementDetails.AverageTradedPrices);

            var dayTradeProfit = profit.DayTradeOperations.Select(x => x.Profit).Sum();
            var swingTradeProfit = profit.SwingTradeOperations.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));
            double totalSold = sells.Sum(stock => stock.OperationValue);

            bool sellsSuperiorThan20000 = totalSold >= AliquotConstants.LimitForStocksSelling;

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && swingTradeProfit > 0) || (dayTradeProfit > 0);

            investorMovementDetails.Assets.Add(new AssetIncomeTaxes
            (
                month, AssetEnumHelper.GetNameByAssetType(Asset.Stocks), profit.OperationHistory
            )
            {
                AssetTypeId = Asset.Stocks,
                Taxes = paysIncomeTaxes ? (double)CalculateTaxesFromProfit(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForStocks) : 0,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit
            });
        }
    }
}
