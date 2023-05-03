﻿using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Calculators.Assets
{
    public class InvestmentsFundsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response, IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForAllMonths(AssetIncomeTaxes response, string month, IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> tickersMovements =
                CalculateAverageTradedPrice(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell) && x.ReferenceDate.ToString("MM") == month);

            double swingTradeProfit = tickersMovements.Where(x => !x.Value.DayTraded && x.Value.Month == month).Select(x => x.Value.Profit).Sum();
            double dayTradeProfit = tickersMovements.Where(x => x.Value.DayTraded && x.Value.Month == month).Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            if (paysIncomeTaxes)
                response.Taxes = (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, IncomeTaxesConstants.IncomeTaxesForInvestmentsFunds);

            bool dayTraded = tickersMovements.Where(x => x.Value.DayTraded && x.Value.Month == month).Any();
            response.DayTraded = dayTraded;

            double totalSold = sells.Sum(bdr => bdr.OperationValue);
            response.TotalSold = totalSold;

            response.SwingTradeProfit = swingTradeProfit;
            response.DayTradeProfit = dayTradeProfit;
            response.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tickersMovements));
            response.AssetTypeId = stocks_infrastructure.Enums.Assets.InvestmentsFunds;
        }
    }
}
