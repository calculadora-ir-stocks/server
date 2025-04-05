namespace Core.Models
{
    public class AverageTradedPriceDetails
    {
        public AverageTradedPriceDetails(string tickerSymbol, double averageTradedPrice, double totalBought, int tradedQuantity, DateTime? referenceDate = null)
        {
            TickerSymbol = tickerSymbol;
            AverageTradedPrice = averageTradedPrice;
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
            ReferenceDate = referenceDate;
        }

        public string TickerSymbol { get; init; }
        public double AverageTradedPrice { get; protected set; }
        public double TotalBought { get; protected set; }
        public int TradedQuantity { get; protected set; }
        public DateTime? ReferenceDate { get; init; }

        public void UpdateAllProperties(double totalBought, int tradedQuantity)
        {
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
            AverageTradedPrice = totalBought / tradedQuantity;
        }

        public void UpdateQuantity(int tradedQuantity)
        {
            TradedQuantity = tradedQuantity;
        }
    }
}
