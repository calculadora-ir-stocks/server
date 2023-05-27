namespace stocks_common.Models
{
    public class TickerAverageTradedPrice
    {
        public TickerAverageTradedPrice(string corporationName, double averageTradedPrice, double totalBought, int tradedQuantity)
        {
            CorporationName = corporationName;
            AverageTradedPrice = averageTradedPrice;
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
        }

        public string CorporationName { get; set; }
        public double AverageTradedPrice { get; set; }
        public double TotalBought { get; set; }
        public int TradedQuantity { get; set; }
    }
}
