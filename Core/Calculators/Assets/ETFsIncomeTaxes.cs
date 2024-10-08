﻿using Common.Enums;
using Common.Helpers;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace Core.Calculators.Assets
{
    public class ETFsIncomeTaxes : ProfitCalculator, IIncomeTaxesCalculator
    {
        public void Execute(
            InvestorMovementDetails investorMovementDetails,
            IEnumerable<Movement.EquitMovement> movements,
            string month
        )
        {
            var profit = CalculateProfitAndAverageTradedPrice(movements, investorMovementDetails.AverageTradedPrices);
            if (profit.TickersBoughtBeforeB3Range.Any())
            {
                // TODO estourar exceção?
            }

            var dayTradeProfit = profit.DayTradeOperations.Select(x => x.Profit).Sum();
            var swingTradeProfit = profit.SwingTradeOperations.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.IsSell());
            double totalSold = sells.Sum(etf => etf.OperationValue);

            decimal taxes = 0;

            if (swingTradeProfit > 0)
                taxes = CalculateTaxesFromProfit(swingTradeProfit, isDayTrade: false, AliquotConstants.IncomeTaxesForETFs);

            if (dayTradeProfit > 0)
                taxes += CalculateTaxesFromProfit(dayTradeProfit, isDayTrade: true, AliquotConstants.IncomeTaxesForDayTrade);

            investorMovementDetails.Assets.Add(new AssetIncomeTaxes(
                month, AssetEnumHelper.GetNameByAssetType(Asset.ETFs), profit.OperationHistory)
            {
                AssetTypeId = Asset.ETFs,
                Taxes = (double)taxes,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit
            });
        }
    }
}
