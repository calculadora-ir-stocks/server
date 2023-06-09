namespace stocks_common.Models
{
    public class OperationDetails
    {
        public OperationDetails(string tickerSymbol, string corporationName, bool tickerBoughtBeforeB3DateRange = false)
        {
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public string TickerSymbol { get; init; }
        public string CorporationName { get; init; }
        public double Profit { get; protected set; } = 0;
        public bool TickerBoughtBeforeB3DateRange { get; protected set; }

        public void UpdateTotalProfit(double profit)
        {
            Profit = profit;
        }

        public void UpdateTickerBoughtBeforeB3DateRange(bool boughtBeforeB3DateRange)
        {
            TickerBoughtBeforeB3DateRange = boughtBeforeB3DateRange;
        }
    }
}
