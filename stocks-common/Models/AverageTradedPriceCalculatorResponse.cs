namespace stocks_common.DTOs.AverageTradedPrice
{
    public class AverageTradedPriceCalculatorResponse
    {
        public AverageTradedPriceCalculatorResponse(double currentPrice, double currentQuantity, string tickerSymbol, string tradeDateTime, bool tickerBoughtBeforeB3DateRange = false)
        {
            CurrentPrice = currentPrice;
            CurrentQuantity = currentQuantity;
            TickerSymbol = tickerSymbol;
            TradeDateTime = tradeDateTime;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public double CurrentPrice { get; set; }
        public double CurrentQuantity { get; set; }
        public string TickerSymbol { get; set; }
        public string TradeDateTime { get; set; }
        public double AverageTradedPrice { get; set; } = 0;
        public double Profit { get; set; } = 0;
        public bool TickerBoughtBeforeB3DateRange { get; set; } = false;
    }
}
