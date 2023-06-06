using Newtonsoft.Json;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators.Assets
{
    public class GoldIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response, IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMovements(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            var (dayTradeOperations, swingTradeOperations) = CalculateProfit(movements);

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));

            double dayTradeProfit = dayTradeOperations.Select(x => x.Value.Profit).Sum();
            double swingTradeProfit = swingTradeOperations.Select(x => x.Value.Profit).Sum();

            bool paysIncomeTaxes = sells.Any() && (swingTradeProfit > 0 || dayTradeProfit > 0);

            response.Add(new AssetIncomeTaxes
            {
                Taxes = TaxesToPay(paysIncomeTaxes, swingTradeProfit, dayTradeProfit),
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit,
                TotalSold = sells.Sum(gold => gold.OperationValue),
                TradedAssets = JsonConvert.SerializeObject(ToDto(movements)),
                AssetTypeId = stocks_infrastructure.Enums.Asset.Gold
            });
        }

        private double TaxesToPay(bool paysIncomeTaxes, double swingTradeProfit, double dayTradeProfit)
        {
            if (paysIncomeTaxes)
                return (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForGold);
            else
                return 0;
        }
    }
}
