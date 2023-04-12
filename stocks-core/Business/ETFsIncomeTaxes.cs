using stocks.Models;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public class ETFsIncomeTaxes : IIncomeTaxesCalculator
    {
        public Task CalculateCurrentMonthIncomeTaxes(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movement, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForAllMonths(CalculateAssetsIncomeTaxesResponse response, IEnumerable<Movement.EquitMovement> movements)
        {
            throw new NotImplementedException();
        }
    }
}
