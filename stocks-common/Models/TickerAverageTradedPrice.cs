namespace stocks_common.Models
{
    public class TickerAverageTradedPrice
    {
        public TickerAverageTradedPrice(string tickerSymbol, double averageTradedPrice, double totalBought, int tradedQuantity)
        {
            TickerSymbol = tickerSymbol;
            AverageTradedPrice = averageTradedPrice;
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
        }

        public string TickerSymbol { get; set; }
        public double AverageTradedPrice { get; set; }
        public double TotalBought { get; set; }
        public int TradedQuantity { get; set; }
    }
}
