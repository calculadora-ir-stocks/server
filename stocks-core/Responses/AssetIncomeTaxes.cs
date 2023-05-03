using stocks_infrastructure.Enums;

namespace stocks_core.Response
{
    public class AssetIncomeTaxes
    {
        /// <summary>
        /// Total a ser pago em imposto de renda referente a um ativo.
        /// </summary>
        public double TotalTaxes { get; set; } = 0;
        /// <summary>
        /// Total vendido (real) do ativo.
        /// </summary>
        public double TotalSold { get; set; } = 0;
        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por swing trade.
        /// </summary>
        public double SwingTradeProfit { get; set; } = 0;
        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por day trade.
        /// </summary>
        public double DayTradeProfit { get; set; } = 0;
        public bool DayTraded { get; set; } = false;
        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string TradedAssets { get; set; } = String.Empty;
        /// <summary>
        /// O id do tipo do ativo sendo negociado.
        /// </summary>
        public Assets AssetTypeId { get; set; }
    }
}
