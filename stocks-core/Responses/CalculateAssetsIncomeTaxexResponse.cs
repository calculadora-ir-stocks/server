namespace stocks_core.Response
{
    public class CalculateAssetsIncomeTaxesResponse
    {
        /// <summary>
        /// Total a ser pago em imposto de renda.
        /// </summary>
        public double TotalIncomeTaxesValue { get; set; } = 0;
        /// <summary>
        /// Total de ativos vendidos.
        /// </summary>
        public double TotalSold { get; set; } = 0;
        public double TotalProfit { get; set; } = 0;
        public bool DayTraded { get; set; } = false;
        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string? Assets { get; set; }
    }
}
