﻿using Common.Enums;
using Common.Helpers;
using Common.Models;
using Core.Constants;
using Core.DTOs.B3;
using Core.Models;

namespace Core.Calculators.Assets
{
    public class ETFsIncomeTaxes : ProfitCalculator, IIncomeTaxesCalculator
    {
        public void Execute(
            List<AssetIncomeTaxes> assetsIncomeTaxes,
            List<AverageTradedPriceDetails> averageTradedPrices,
            IEnumerable<Movement.EquitMovement> movements,
            string month
        )
        {
            var (dayTradeOperations, swingTradeOperations) = CalculateProfit(movements, averageTradedPrices);

            double dayTradeProfit = dayTradeOperations.Select(x => x.Profit).Sum();
            double swingTradeProfit = swingTradeOperations.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));
            double totalSold = sells.Sum(etf => etf.OperationValue);

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            assetsIncomeTaxes.Add(new AssetIncomeTaxes
            (
                month, AssetTypeHelper.GetNameByAssetType(Asset.ETFs), GetOperationDetails()
            )
            {
                AssetTypeId = Asset.ETFs,
                Taxes = paysIncomeTaxes ? (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForETFs) : 0,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit
            });
        }
    }
}