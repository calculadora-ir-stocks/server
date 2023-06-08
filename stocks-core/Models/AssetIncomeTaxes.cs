using stocks_common.Enums;
using stocks_common.Models;

namespace stocks_core.Models
{
    public class AssetIncomeTaxes
    {
        /// <summary>
        /// O id do tipo de ativo sendo negociado.
        /// </summary>
        public Asset AssetTypeId { get; set; }

        /// <summary>
        /// Total a ser pago em imposto de renda referente a um ativo.
        /// </summary>
        public double Taxes { get; set; } = 0;

        /// <summary>
        /// Total vendido do ativo.
        /// </summary>
        public double TotalSold { get; set; } = 0;

        /// <summary>
        /// Lista de KeyValuePair (tickerSymbol, AverageTradedPriceDetails) contendo o preço médio de cada ativo negociado.
        /// </summary>
        public IList<KeyValuePair<string, AverageTradedPriceDetails>> AverageTradedPrices { get; init; } = null!;

        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por swing trade.
        /// </summary>
        public double SwingTradeProfit { get; set; } = 0;

        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por day trade.
        /// </summary>
        public double DayTradeProfit { get; set; } = 0;

        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string TradedAssets { get; set; } = string.Empty;
    }
}
