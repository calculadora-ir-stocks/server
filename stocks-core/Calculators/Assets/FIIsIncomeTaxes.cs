using Newtonsoft.Json;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Calculators.Assets
{
    public class FIIsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response, IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMonth(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            var tradedTickersDetails = CalculateMovements(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double swingTradeProfit = tradedTickersDetails.Where(x => !x.Value.DayTraded).Select(x => x.Value.Profit).Sum();
            double dayTradeProfit = tradedTickersDetails.Where(x => x.Value.DayTraded).Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            response.Add(new AssetIncomeTaxes
            {
                Taxes = TaxesToPay(paysIncomeTaxes, swingTradeProfit, dayTradeProfit),
                DayTraded = DayTraded(tradedTickersDetails),
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit,
                TotalSold = sells.Sum(fii => fii.OperationValue),
                AverageTradedPrices = GetAssetDetails(),
                TradedAssets = JsonConvert.SerializeObject(DictionaryToList(tradedTickersDetails)),
                AssetTypeId = stocks_infrastructure.Enums.Assets.FIIs
            });
        }

        private bool DayTraded(Dictionary<string, TickerDetails> tradedTickersDetails)
        {
            return tradedTickersDetails.Where(x => x.Value.DayTraded).Any();
        }

        private double TaxesToPay(bool paysIncomeTaxes, double swingTradeProfit, double dayTradeProfit)
        {
            if (paysIncomeTaxes)
                return (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, IncomeTaxesConstants.IncomeTaxesForFIIs);
            else
                return 0;
        }
    }
}
