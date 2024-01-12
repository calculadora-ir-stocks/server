using Common.Enums;
using Common.Helpers;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace Core.Calculators.Assets
{
    public class FIIsIncomeTaxes : ProfitCalculator, IIncomeTaxesCalculator
    {
        public void Execute(
            InvestorMovementDetails investorMovementDetails,
            IEnumerable<Movement.EquitMovement> movements,
            string month
        )
        {
            var response = CalculateProfit(movements, investorMovementDetails.AverageTradedPrices);

            var dayTradeProfit = response.DayTradeOperations.Select(x => x.Profit).Sum();
            var swingTradeProfit = response.SwingTradeOperations.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));
            double totalSold = sells.Sum(fii => fii.OperationValue);

            decimal taxes = 0;

            if (swingTradeProfit > 0)
                taxes = CalculateTaxesFromProfit(swingTradeProfit, isDayTrade: false, AliquotConstants.IncomeTaxesForFIIs);

            if (dayTradeProfit > 0)
                taxes += CalculateTaxesFromProfit(dayTradeProfit, isDayTrade: true, AliquotConstants.IncomeTaxesForDayTrade);

            investorMovementDetails.Assets.Add(new AssetIncomeTaxes(
                month, AssetEnumHelper.GetNameByAssetType(Asset.FIIs), response.OperationHistory)
            {
                AssetTypeId = Asset.FIIs,
                Taxes = (double)taxes,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit
            });
        }
    }
}
