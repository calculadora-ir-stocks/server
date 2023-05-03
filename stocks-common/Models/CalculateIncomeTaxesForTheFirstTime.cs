namespace stocks_common.Models
{
    public class CalculateIncomeTaxesForTheFirstTime
    {
        public CalculateIncomeTaxesForTheFirstTime(double currentPrice, double currentQuantity, string tickerSymbol, string corporationName,
            double averageTradedPrice, bool dayTraded, string month, bool tickerBoughtBeforeB3DateRange = false)
        {
            Price = currentPrice;
            Quantity = currentQuantity;
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            AverageTradedPrice = averageTradedPrice;
            DayTraded = dayTraded;
            Month = month;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public double Price { get; set; }
        public double Quantity { get; set; }
        public string TickerSymbol { get; set; }
        public string CorporationName { get; set; }
        public double AverageTradedPrice { get; set; } = 0;
        public double Profit { get; set; } = 0;
        public bool DayTraded { get; set; }
        public string Month { get; set; }
        public bool TickerBoughtBeforeB3DateRange { get; set; } = false;
    }
}
