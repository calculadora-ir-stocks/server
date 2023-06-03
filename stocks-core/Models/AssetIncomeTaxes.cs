using stocks_common.Models;
using stocks_infrastructure.Enums;

namespace stocks_core.Models
{
    public class AssetIncomeTaxes
    {
        /// <summary>
        /// Total a ser pago em imposto de renda.
        /// </summary>
        public double Taxes { get; set; } = 0;

        /// <summary>
        /// Total vendido do ativo.
        /// </summary>
        public double TotalSold { get; set; } = 0;

        /// <summary>
        /// O preço médio dos ativos.
        /// </summary>
        public Dictionary<string, TickerAverageTradedPrice> AverageTradedPrices { get; set; } = new();

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

        /// <summary>
        /// O id do tipo de ativo sendo negociado.
        /// </summary>
        public Assets AssetTypeId { get; set; }
    }
}
