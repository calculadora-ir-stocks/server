using Core.Models;
using Core.Models.B3;

namespace Core.Calculators
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Calcula o imposto de renda e o preço médio com base nas movimentações especificadas.
        /// <para>Altera o <see cref="InvestorMovementDetails.Assets"/> com o imposto devido referente as movimentações <see cref="movements"/>.</para>
        /// <para>Altera o <see cref="InvestorMovementDetails.AverageTradedPrices"/> com o preço médio dos ativos negociados referente as movimentações
        /// <see cref="movements"/>.</para>
        /// </summary>
        void Execute(InvestorMovementDetails investorMovementDetails, IEnumerable<Movement.EquitMovement> movements, string month);
    }
}
