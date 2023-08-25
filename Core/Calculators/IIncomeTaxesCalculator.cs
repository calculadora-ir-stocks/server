using Common.Models;
using Core.DTOs.B3;
using Core.Models;

namespace Core.Calculators
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Calcula o imposto de renda e o preço médios com base nas movimentações especificadas.
        /// <para>Altera o <c>assets</c> com o imposto devido referente as movimentações <c>movements</c>.</para>
        /// <para>Altera o <c>averageTradedPrices</c> com o preço médio dos ativos negociados referente as movimentações <c>movements</c>.</para>
        /// </summary>
        void Execute(List<AssetIncomeTaxes> assets, List<AverageTradedPriceDetails> averageTradedPrices,
            IEnumerable<Movement.EquitMovement> movements, string month);
    }
}
