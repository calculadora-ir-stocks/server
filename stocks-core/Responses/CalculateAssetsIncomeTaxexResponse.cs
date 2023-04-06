namespace stocks_core.Response
{
    public class CalculateAssetsIncomeTaxesResponse
    {
        /// <summary>
        /// Total a ser pago em imposto de renda.
        /// </summary>
        public double TotalIncomeTaxesValue { get; set; } = 0;
        public IEnumerable<Asset> Assets { get; set; } = Array.Empty<Asset>();
    }

    public class Asset
    {
        public Asset(string ticker, int tradeQuantity, string tradeDateTime, double totalIncomeTaxesValue)
        {
            Ticker = ticker;
            TradeQuantity = tradeQuantity;
            TradeDateTime = tradeDateTime;
            TotalIncomeTaxesValue = totalIncomeTaxesValue;
        }

        public Asset() { }

        public string Ticker { get; set; }
        public int TradeQuantity { get; set; }
        public string TradeDateTime { get; set; }

        /// <summary>
        /// Total a ser pago em imposto de renda (referente a um ativo).
        /// </summary>
        public double TotalIncomeTaxesValue { get; set; }
    }
}
