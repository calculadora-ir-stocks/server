using stocks_common.Models;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Calcula o total de imposto a ser pago em operações swing-trade e day-trade.
        /// </summary>
        /// <param name="response">Variável que será atualizada com o imposto devido e lucro em operações swing-trade e day-trade</param>
        /// <param name="averageTradedPrices">Preço médio de todos os ativos negociados</param>
        void CalculateIncomeTaxes(List<AssetIncomeTaxes> response, List<AverageTradedPriceDetails> averageTradedPrices, 
            IEnumerable<Movement.EquitMovement> movements, string month);
    }
}
