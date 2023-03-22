using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Adiciona no objeto CalculateAssetsIncomeTaxesResponse os ativos e seus respectivos impostos de renda a serem pagos.
        /// </summary>
        Task AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId);
    }
}
