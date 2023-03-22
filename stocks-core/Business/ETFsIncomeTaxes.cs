using stocks.Models;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public class ETFsIncomeTaxes : IIncomeTaxesCalculator
    {
        public static List<Movement.EquitMovement> etfs = new();

        public Task AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movement, Guid accountId)
        {
            throw new NotImplementedException();
        }
    }
}
