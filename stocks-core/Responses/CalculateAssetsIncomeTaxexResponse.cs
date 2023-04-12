namespace stocks_core.Response
{
    public class CalculateAssetsIncomeTaxesResponse
    {
        /// <summary>
        /// Total a ser pago em imposto de renda.
        /// </summary>
        public double TotalIncomeTaxesValue { get; set; } = 0;
        /// <summary>
        /// Total de prejuízos a serem compensados nos meses seguintes.
        /// </summary>
        public double CompensateLoss { get; set; } = 0;
        public IEnumerable<Asset> Assets { get; set; } = Array.Empty<Asset>();
    }

    public class Asset
    {
        public Asset(string ticker, double averageTradedPrice)
        {
            Ticker = ticker;
            AverageTradedPrice = averageTradedPrice;
        }

        public string Ticker { get; set; }
        public double AverageTradedPrice { get; set; }
    }
}
