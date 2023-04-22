namespace stocks_core.Response
{
    public class AssetIncomeTaxes
    {
        /// <summary>
        /// Total a ser pago em imposto de renda referente a um ativo.
        /// </summary>
        public double TotalTaxes { get; set; } = 0;
        /// <summary>
        /// Total de ativos vendidos.
        /// </summary>
        public double TotalSold { get; set; } = 0;
        public double TotalProfit { get; set; } = 0;
        public bool DayTraded { get; set; } = false;
        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string? TradedAssets { get; set; }
        /// <summary>
        /// O id do tipo do ativo sendo negociado.
        /// </summary>
        public int AssetTypeId { get; set; }
    }
}
