using Core.Models;
using Core.Models.B3;

namespace Core.Calculators
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Calcula o imposto de renda e o preço médio com base nas movimentações especificadas. <br/> <br/>
        /// Altera o <see cref="InvestorMovementDetails.Assets"/> com o imposto devido referente as movimentações <c>movements</c>. <br/>
        /// Altera o <see cref="InvestorMovementDetails.AverageTradedPrices"/> com o preço médio dos ativos negociados referente as movimentações <c>movements</c>.
        /// </summary>
        void Execute(InvestorMovementDetails investorMovementDetails, IEnumerable<Movement.EquitMovement> movements, string month);
    }
}
