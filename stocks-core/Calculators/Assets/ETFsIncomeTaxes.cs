using Newtonsoft.Json;
using stocks_common.Enums;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators.Assets
{
    public class ETFsIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> movement, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMovements(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            var (dayTradeOperations, swingTradeOperations) = CalculateProfit(movements);


            double dayTradeProfit = dayTradeOperations.Select(x => x.Value.Profit).Sum();
            double swingTradeProfit = swingTradeOperations.Select(x => x.Value.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));
            double totalSold = sells.Sum(etf => etf.OperationValue);

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            response.Add(new AssetIncomeTaxes
            {
                AssetTypeId = Asset.ETFs,
                Taxes = paysIncomeTaxes ? (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForETFs) : 0,
                TotalSold = totalSold,
                AverageTradedPrices = GetAverageTradedPrice(Asset.ETFs).ToList(),
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit,
                TradedAssets = JsonConvert.SerializeObject(ToDto(movements, B3ResponseConstants.ETFs)),
            });
        }
    }
}
