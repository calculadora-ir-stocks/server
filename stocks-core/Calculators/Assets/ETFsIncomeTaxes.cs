using Newtonsoft.Json;
using stocks_common.Enums;
using stocks_common.Models;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators.Assets
{
    public class ETFsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateIncomeTaxes(
            List<AssetIncomeTaxes> response,
            List<AverageTradedPriceDetails> averageTradedPrices,
            IEnumerable<Movement.EquitMovement> movements,
            string month
        )
        {
            var (dayTradeOperations, swingTradeOperations) = CalculateProfit(movements);

            double dayTradeProfit = dayTradeOperations.Select(x => x.Profit).Sum();
            double swingTradeProfit = swingTradeOperations.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));
            double totalSold = sells.Sum(etf => etf.OperationValue);

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            response.Add(new AssetIncomeTaxes(month)
            {
                AssetTypeId = Asset.ETFs,
                Taxes = paysIncomeTaxes ? (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForETFs) : 0,
                TotalSold = totalSold,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit,
                TradedAssets = JsonConvert.SerializeObject(ToDto(movements, B3ResponseConstants.ETFs)),
            });

            AddIntoAverageTradedPricesList(averageTradedPrices, Asset.ETFs);
        }
    }
}
