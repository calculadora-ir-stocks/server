using stocks_common.Models;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Services.IncomeTaxes
{
    public interface IIncomeTaxesService
    {
        /// <summary>
        /// Retorna o imposto de renda a ser pago nas movimentações especificadas e o preço médio
        /// de cada ativo movimentado.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<(List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>)> Execute(Movement.Root? request, Guid accountId);
    }
}
