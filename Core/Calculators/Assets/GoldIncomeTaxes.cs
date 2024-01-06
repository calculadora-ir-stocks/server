﻿using Common.Enums;
using Common.Helpers;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace Core.Calculators.Assets
{
    public class GoldIncomeTaxes : ProfitCalculator, IIncomeTaxesCalculator
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
            double totalSold = sells.Sum(gold => gold.OperationValue);

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            investorMovementDetails.Assets.Add(new AssetIncomeTaxes(
                month, AssetEnumHelper.GetNameByAssetType(Asset.Gold), response.OperationHistory)
            {
                AssetTypeId = Asset.Gold,
                Taxes = paysIncomeTaxes ? (double)CalculateTaxesFromProfit(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForGold) : 0,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit
            });
        }
    }
}
