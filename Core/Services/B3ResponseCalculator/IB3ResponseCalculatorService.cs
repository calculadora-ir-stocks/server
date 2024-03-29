﻿using Core.Models;
using Core.Models.B3;

namespace Core.Services.IncomeTaxes
{
    public interface IB3ResponseCalculatorService
    {
        /// <summary>
        /// Com base no objeto de retorno da B3, calcula o imposto de renda a ser pago nas movimentações especificadas de cada mês
        /// e o preço médio de cada ativo movimentado.
        /// </summary>
        /// <param name="request">O objeto de retorno da B3.</param>
        /// <param name="accountId">O id do usuário.</param>
        /// <returns>Detalhe dos impostos de cada mês e o preço médio de cada ativo negociado.</returns>
        Task<InvestorMovementDetails?> Calculate(Movement.Root? request, Guid accountId);
    }
}
