using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public class ETFsIncomeTaxes : IIncomeTaxesCalculator
    {
        public void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> movement, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public void CalculateIncomeTaxesForAllMonths(List<AssetIncomeTaxes> response, IEnumerable<Movement.EquitMovement> movements)
        {
            throw new NotImplementedException();
        }
    }
}
