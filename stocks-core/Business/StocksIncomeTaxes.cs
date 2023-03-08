using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public class StocksIncomeTaxes : IIncomeTaxesCalculation
    {
        private static double totalSoldInStocks = 0.00;
        /// <summary>
        /// Algoritmo para calcular o imposto de renda a ser pago em ações.
        /// </summary>
        public async Task<CalculateAssetsIncomeTaxesResponse?> AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            Movement.EquitMovement? movement)
        {
            totalSoldInStocks += movement.OperationValue;

            if (totalSoldInStocks < IncomeTaxesConstants.LimitForStocksSelling)
                return null;



            await Task.Delay(0);
            return new CalculateAssetsIncomeTaxesResponse();
        }
    }
}
