using stocks_core.DTOs.AverageTradedPrice;
using stocks_core.DTOs.B3;

namespace stocks_core.Services.AverageTradedPrice
{
    public interface IAverageTradedPriceService
    {
        /// <summary>
        /// Calcula o preço médio de todos os ativos do investidor, levando em consideração todas as movimentações
        /// de 01-11-2019 até D-1.
        /// 
        /// Deve ser rodado apenas na primeira vez que o usuário acessa a plataforma.
        /// </summary>
        public Dictionary<string, AverageTradedPriceCalculator> CalculateAverageTradedPrice(
            IEnumerable<Movement.EquitMovement> buyOperations,
            IEnumerable<Movement.EquitMovement> sellOperations,
            IEnumerable<Movement.EquitMovement> splitsOperations,
            IEnumerable<Movement.EquitMovement> bonusSharesOperations
        );
    }
}
