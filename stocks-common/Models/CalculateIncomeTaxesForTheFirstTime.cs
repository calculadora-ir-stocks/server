namespace stocks_common.Models
{
    public class CalculateIncomeTaxesForTheFirstTime
    {
        public CalculateIncomeTaxesForTheFirstTime(double currentPrice, double currentQuantity, string tickerSymbol, string corporationName, string tradeDateTime,
            double averageTradedPrice, bool dayTraded, bool tickerBoughtBeforeB3DateRange = false)
        {
            Price = currentPrice;
            Quantity = currentQuantity;
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            TradeDateTime = tradeDateTime;
            AverageTradedPrice = averageTradedPrice;
            DayTraded = dayTraded;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public double Price { get; set; }
        public double Quantity { get; set; }
        public string TickerSymbol { get; set; }
        public string CorporationName { get; set; }
        public string TradeDateTime { get; set; }
        public double AverageTradedPrice { get; set; } = 0;
        public double Profit { get; set; } = 0;
        public bool DayTraded { get; set; }
        public bool TickerBoughtBeforeB3DateRange { get; set; } = false;
    }
}
