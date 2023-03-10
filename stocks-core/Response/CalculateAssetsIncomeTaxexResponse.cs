namespace stocks_core.Response
{
    public class CalculateAssetsIncomeTaxesResponse
    {
        /// <summary>
        /// Total a ser pago em imposto de renda.
        /// </summary>
        public double TotalIncomeTaxesValue { get; set; }
        public List<Asset> Assets { get; set; }
    }

    public class Asset
    {
        public string Ticker { get; set; }
        public int TradeQuantity { get; set; }
        public double GrossAmount { get; set; }
        public DateTime TradeDateTime { get; set; }

        /// <summary>
        /// Total a ser pago em imposto de renda (referente a um ativo).
        /// </summary>
        public double TotalIncomeTaxesValue { get; set; }
    }
}
