using stocks_common.Models;
using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Calculators.Assets
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Adiciona no objeto CalculateAssetsIncomeTaxesResponse os ativos e seus respectivos impostos de renda a serem pagos.
        /// </summary>
        void CalculateCurrentMonthIncomeTaxes(AssetIncomeTaxes? response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId);

        /// <summary>
        /// Altera a variável response com o imposto de renda a ser pago referente as movimentações especificadas.
        /// Retorna o preço médio de todos os ativos movimentados do investidor.
        /// </summary>
        /// <returns>Lista contendo o preço médio de todos os ativos negociados.</returns>
        void CalculateIncomeTaxesForSpecifiedMonth(AssetIncomeTaxes response, IEnumerable<Movement.EquitMovement> movements);

        /// <summary>
        /// Retorna uma lista de tickers e seus respectivos preços médios.
        /// Essa função deve ser chamada após a CalculateIncomeTaxesForSpecifiedMonth, já que ela que
        /// define os preços médios.
        /// </summary>
        /// <returns></returns>
        List<TickerAverageTradedPrice> GetTickersAverageTradedPrice();
    }
}
