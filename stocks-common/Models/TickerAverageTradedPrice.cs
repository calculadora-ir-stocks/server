namespace stocks_common.Models
{
    public class TickerAverageTradedPrice
    {
        public TickerAverageTradedPrice(string tickerSymbol, string corporationName, double averageTradedPrice, double totalBought, int tradedQuantity)
        {
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            AverageTradedPrice = averageTradedPrice;
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
        }

        public string TickerSymbol { get; protected set; }
        public string CorporationName { get; set; }
        public double AverageTradedPrice { get; set; }
        public double TotalBought { get; set; }
        public int TradedQuantity { get; set; }
    }
}
