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
        /// O total de lucro ou prejuízo de um determinado ativo.
        /// </summary>
        public double TotalProfitOrLoss { get; set; } = 0;
        public bool DayTraded { get; set; } = false;
        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string TradedAssets { get; set; } = String.Empty;
        /// <summary>
        /// O id do tipo do ativo sendo negociado.
        /// </summary>
        public int AssetTypeId { get; set; }
    }
}
