using stocks.Models;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public class ETFsIncomeTaxes : IIncomeTaxesCalculator
    {
        public static List<Movement.EquitMovement> etfs = new();

        public Task CalculateCurrentMonthIncomeTaxes(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movement, Guid accountId)
        {
            throw new NotImplementedException();
        }

        void IIncomeTaxesCalculator.CalculateIncomeTaxesForAllMonths(CalculateAssetsIncomeTaxesResponse response, IEnumerable<Movement.EquitMovement> movements, Guid accountId)
        {
            throw new NotImplementedException();
        }
    }
}
