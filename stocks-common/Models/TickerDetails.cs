namespace stocks_common.Models
{
    public class TickerDetails
    {
        public TickerDetails(double currentPrice, double currentQuantity, string tickerSymbol, string corporationName,
            bool dayTraded, bool tickerBoughtBeforeB3DateRange = false)
        {
            Price = currentPrice;
            Quantity = currentQuantity;
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            DayTraded = dayTraded;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public TickerDetails() { }

        public double Price { get; set; }
        public double Quantity { get; set; }
        public string TickerSymbol { get; set; }
        public string CorporationName { get; set; }
        public double Profit { get; set; } = 0;
        public bool DayTraded { get; set; } = false;
        public bool TickerBoughtBeforeB3DateRange { get; set; } = false;
    }
}
