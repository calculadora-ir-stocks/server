using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public class ETFsIncomeTaxes : IIncomeTaxesCalculation
    {
        public Task<CalculateAssetsIncomeTaxesResponse?> AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            Movement.EquitMovement? movement)
        {
            throw new NotImplementedException();
        }
    }
}
