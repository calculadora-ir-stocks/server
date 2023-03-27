using stocks_core.DTOs.AverageTradedPrice;
using stocks_core.DTOs.B3;

namespace stocks_core.Services.AverageTradedPrice
{
    public interface IAverageTradedPriceService
    {
        /// <summary>
        /// Calcula o preço médio de um ativo com base nas operações de compra, venda, desdobro e bonificações.
        /// </summary>
        public Dictionary<string, AverageTradedPriceCalculatorResponse> CalculateAverageTradedPrice(
            IEnumerable<Movement.EquitMovement> buyOperations,
            IEnumerable<Movement.EquitMovement> sellOperations,
            IEnumerable<Movement.EquitMovement> splitsOperations,
            IEnumerable<Movement.EquitMovement> bonusSharesOperations
        );
    }
}
