using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_infrastructure.Enums;

namespace stocks_core.Business
{
    public class ETFsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> movement, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForAllMonths(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> tickersMovementsDetails = 
                CalculateAverageTradedPrice(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));
            double totalProfit = tickersMovementsDetails.Select(x => x.Value.Profit).Sum();

            bool dayTraded = InvestorDayTraded(movements);

            bool paysIncomeTaxes = (sells.Any() && totalProfit > 0) || (dayTraded && totalProfit > 0);

            AssetIncomeTaxes objectToAddIntoResponse = new();

            if (paysIncomeTaxes)
                objectToAddIntoResponse.TotalTaxes = (double)CalculateIncomeTaxes(totalProfit, dayTraded, IncomeTaxesConstants.IncomeTaxesForETFs);

            objectToAddIntoResponse.DayTraded = dayTraded;
            objectToAddIntoResponse.TotalProfitOrLoss = totalProfit;

            double totalSold = sells.Sum(stock => stock.OperationValue);
            objectToAddIntoResponse.TotalSold = totalSold;

            objectToAddIntoResponse.TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tickersMovementsDetails));
            objectToAddIntoResponse.AssetTypeId = (int)Assets.Stocks;

            response.Add(objectToAddIntoResponse);
        }
    }
}
