namespace stocks_common.Models
{
    public class OperationDetails
    {
        public OperationDetails(string tickerSymbol, string corporationName, bool dayTraded, bool tickerBoughtBeforeB3DateRange = false)
        {
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            DayTraded = dayTraded;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public string TickerSymbol { get; protected set; }
        public string CorporationName { get; protected set; }
        public double Profit { get; protected set; } = 0;
        public bool DayTraded { get; protected set; } = false;
        public bool TickerBoughtBeforeB3DateRange { get; protected set; }

        public void UpdateTotalProfit(double profit)
        {
            Profit = profit;
        }
    }
}
