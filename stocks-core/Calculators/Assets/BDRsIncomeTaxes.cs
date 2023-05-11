﻿using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Calculators.Assets
{
    public class BDRsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response, IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMonth(AssetIncomeTaxes response, IEnumerable<Movement.EquitMovement> movements)
        {            
            var tickersMovements = CalculateMovements(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double swingTradeProfit = tickersMovements.Where(x => !x.Value.DayTraded).Select(x => x.Value.Profit).Sum();
            double dayTradeProfit = tickersMovements.Where(x => x.Value.DayTraded ).Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            if (paysIncomeTaxes)
                response.Taxes = (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, IncomeTaxesConstants.IncomeTaxesForBDRs);

            bool dayTraded = tickersMovements.Where(x => x.Value.DayTraded).Any();
            response.DayTraded = dayTraded;

            double totalSold = sells.Sum(bdr => bdr.OperationValue);
            response.TotalSold = totalSold;

            response.SwingTradeProfit = swingTradeProfit;
            response.DayTradeProfit = dayTradeProfit;
            response.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tickersMovements));
            response.AssetTypeId = stocks_infrastructure.Enums.Assets.BDRs;
        }

        public List<TickerAverageTradedPrice> GetTickersAverageTradedPrice()
        {
            return GetTickersAverageTradedPrice();
        }
    }
}
