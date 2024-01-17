namespace Core.Models
{
    public class AverageTradedPriceDetails
    {
        public AverageTradedPriceDetails(string tickerSymbol, double averageTradedPrice, double totalBought, int tradedQuantity)
        {
            TickerSymbol = tickerSymbol;
            AverageTradedPrice = averageTradedPrice;
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
        }

        public string TickerSymbol { get; init; }
        public double AverageTradedPrice { get; protected set; }
        public double TotalBought { get; protected set; }
        public int TradedQuantity { get; protected set; }


        public void UpdateAverageTradedPrice(double totalBought, int tradedQuantity)
        {
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
            AverageTradedPrice = totalBought / tradedQuantity;
        }
    }
}
