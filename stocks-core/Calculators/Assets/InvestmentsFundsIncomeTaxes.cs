using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators.Assets
{
    public class InvestmentsFundsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response, IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMovements(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            //var dayTradeOperations = GetDayTradeOperations(movements);
            //var swingTradeOperations = GetSwingTradeOperations(movements);

            //var tradedTickersDetails = CalculateMovements(dayTradeOperations, swingTradeOperations);

            //var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));

            //double swingTradeProfit = tradedTickersDetails.Where(x => !x.Value.DayTraded).Select(x => x.Value.Profit).Sum();
            //double dayTradeProfit = tradedTickersDetails.Where(x => x.Value.DayTraded).Select(x => x.Value.Profit).Sum();

            //bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            //response.Add(new AssetIncomeTaxes
            //{
            //    Taxes = TaxesToPay(paysIncomeTaxes, swingTradeProfit, dayTradeProfit),
            //    DayTraded = DayTraded(tradedTickersDetails),
            //    SwingTradeProfit = swingTradeProfit,
            //    DayTradeProfit = dayTradeProfit,
            //    TotalSold = sells.Sum(funds => funds.OperationValue),
            //    AverageTradedPrices = GetAssetDetails(),
            //    TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tradedTickersDetails)),
            //    AssetTypeId = stocks_infrastructure.Enums.Assets.InvestmentsFunds
            //});
        }

        private bool DayTraded(Dictionary<string, OperationDetails> tradedTickersDetails)
        {
            return tradedTickersDetails.Where(x => x.Value.DayTraded).Any();
        }

        private double TaxesToPay(bool paysIncomeTaxes, double swingTradeProfit, double dayTradeProfit)
        {
            if (paysIncomeTaxes)
                return (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForInvestmentsFunds);
            else
                return 0;
        }
    }
}
