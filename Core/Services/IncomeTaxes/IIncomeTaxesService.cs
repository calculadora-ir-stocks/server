using Core.Models;
using Core.Models.B3;

namespace Core.Services.IncomeTaxes
{
    public interface IIncomeTaxesService
    {
        /// <summary>
        /// Com base no objeto de retorno Root da B3, calcula o imposto de renda a ser pago nas movimentações especificadas de cada mnês
        /// e o preço médio de cada ativo movimentado.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="accountId"></param>
        /// <returns>Detalhe dos impostos de cada mês e o preço médio de cada ativo negociado.</returns>
        Task<InvestorMovementDetails?> GetB3ResponseDetails(Movement.Root? request, Guid accountId);
    }
}
