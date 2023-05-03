using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Calculators.Assets
{
    public class ETFsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> movement, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForAllMonths(List<AssetIncomeTaxes> response, string month, IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> tickersMovements =
                CalculateAverageTradedPrice(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell) && x.ReferenceDate.ToString("MM") == month);

            double swingTradeProfit = tickersMovements.Where(x => !x.Value.DayTraded && x.Value.Month == month).Select(x => x.Value.Profit).Sum();
            double dayTradeProfit = tickersMovements.Where(x => x.Value.DayTraded && x.Value.Month == month).Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            AssetIncomeTaxes objectToAddIntoResponse = new();

            if (paysIncomeTaxes)
                objectToAddIntoResponse.TotalTaxes = (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, IncomeTaxesConstants.IncomeTaxesForETFs);

            bool dayTraded = tickersMovements.Where(x => x.Value.DayTraded && x.Value.Month == month).Any();
            objectToAddIntoResponse.DayTraded = dayTraded;

            double totalSold = sells.Sum(etf => etf.OperationValue);
            objectToAddIntoResponse.TotalSold = totalSold;

            objectToAddIntoResponse.TotalProfitOrLoss = swingTradeProfit + dayTradeProfit;
            objectToAddIntoResponse.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tickersMovements));
            objectToAddIntoResponse.AssetTypeId = stocks_infrastructure.Enums.Assets.ETFs;

            response.Add(objectToAddIntoResponse);
        }
    }
}
