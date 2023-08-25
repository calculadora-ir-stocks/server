using Common.Models;
using Core.DTOs.B3;
using Core.Models;

namespace Core.Services.IncomeTaxes
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
