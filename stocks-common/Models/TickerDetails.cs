namespace stocks_common.Models
{
    public class TickerDetails
    {
        public TickerDetails(double price, double quantity, string tickerSymbol, string corporationName,
            double averageTradedPrice, bool dayTraded, bool tickerBoughtBeforeB3DateRange = false)
        {
            Price = price;
            Quantity = quantity;
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            AverageTradedPrice = averageTradedPrice;
            DayTraded = dayTraded;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public double Price { get; set; }
        public double Quantity { get; set; }
        public string TickerSymbol { get; set; }
        public string CorporationName { get; set; }
        public double AverageTradedPrice { get; set; }
        public double Profit { get; set; } = 0;
        public bool DayTraded { get; set; } = false;
        public bool TickerBoughtBeforeB3DateRange { get; set; } = false;
    }
}
