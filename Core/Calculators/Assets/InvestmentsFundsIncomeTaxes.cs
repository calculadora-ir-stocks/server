using Common.Enums;
using Common.Helpers;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace Core.Calculators.Assets
{
    public class InvestmentsFundsIncomeTaxes : ProfitCalculator, IIncomeTaxesCalculator
    {
        public void Execute(
            InvestorMovementDetails investorMovementDetails,
            IEnumerable<Movement.EquitMovement> movements,
            string month
        )
        {
            var profit = CalculateProfitAndAverageTradedPrice(movements, investorMovementDetails.AverageTradedPrices, month);
            if (profit.TickersBoughtBeforeB3Range.Any())
            {
                // TODO estourar exceção?
            }

            var dayTradeProfit = profit.DayTradeOperations.Select(x => x.Profit).Sum();
            var swingTradeProfit = profit.SwingTradeOperations.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.IsSell());
            double totalSold = sells.Sum(funds => funds.OperationValue);

            decimal taxes = 0;

            if (swingTradeProfit > 0)
                taxes = CalculateTaxesFromProfit(swingTradeProfit, isDayTrade: false, AliquotConstants.IncomeTaxesForInvestmentsFunds);

            if (dayTradeProfit > 0)
                taxes += CalculateTaxesFromProfit(dayTradeProfit, isDayTrade: true, AliquotConstants.IncomeTaxesForInvestmentsFunds);

            investorMovementDetails.Assets.Add(new AssetIncomeTaxes(
                month, AssetEnumHelper.GetNameByAssetType(Asset.InvestmentsFunds), profit.OperationHistory)
            {
                AssetTypeId = Asset.InvestmentsFunds,
                Taxes = (double)taxes,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit
            });
        }
    }
}
