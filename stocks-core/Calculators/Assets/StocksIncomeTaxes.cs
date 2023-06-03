using Newtonsoft.Json;
using stocks_core.Business;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators.Assets
{
    public class StocksIncomeTaxes : AverageTradedPriceCalculator, IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForSpecifiedMovements(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            var (dayTradeOperations, swingTradeOperations) = CalculateMovements(movements);

            var dayTradeProfit = dayTradeOperations.Values.Select(x => x.Profit).Sum();
            var swingTradeProfit = swingTradeOperations.Values.Select(x => x.Profit).Sum();

            var sells = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell));

            double totalSold = sells.Sum(stock => stock.OperationValue);
            bool sellsSuperiorThan20000 = totalSold >= AliquotConstants.LimitForStocksSelling;

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && swingTradeProfit > 0) || (dayTradeProfit > 0);

            response.Add(new AssetIncomeTaxes
            {
                Taxes = paysIncomeTaxes ? TaxesToPay(swingTradeProfit, dayTradeProfit) : 0,
                SwingTradeProfit = swingTradeProfit,
                DayTradeProfit = dayTradeProfit,
                TotalSold = totalSold,
                AverageTradedPrices = GetAssetDetails(),
                TradedAssets = JsonConvert.SerializeObject(DictionaryToList(dayTradeOperations)), // TODO CHANCE THIS CONCAT TWO DICTIONARIES
                AssetTypeId = stocks_infrastructure.Enums.Assets.Stocks
            });
        }

        private double TaxesToPay(double swingTradeProfit, double dayTradeProfit)
        {
            return (double)CalculateIncomeTaxes(swingTradeProfit, dayTradeProfit, AliquotConstants.IncomeTaxesForStocks);
        }
    }
}
