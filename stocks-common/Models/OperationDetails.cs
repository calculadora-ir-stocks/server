using Newtonsoft.Json;

namespace stocks_common.Models
{
    public class OperationDetails
    {
        public OperationDetails(string day, string tickerSymbol, string corporationName, bool tickerBoughtBeforeB3DateRange = false)
        {
            Day = day;
            TickerSymbol = tickerSymbol;
            CorporationName = corporationName;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public string Day { get; protected set; }
        public string TickerSymbol { get; init; }
        public string CorporationName { get; init; }
        public double Profit { get; protected set; } = 0;
        public double TotalSold { get; protected set; } = 0;

        [JsonIgnore]
        public bool TickerBoughtBeforeB3DateRange { get; protected set; }

        public void UpdateProfit(double profit, string day)
        {
            Day = day;
            Profit = profit;
        }

        public void UpdateTotalSold(double totalSold)
        {
            TotalSold = totalSold;
        }

        public void UpdateTickerBoughtBeforeB3DateRange(bool boughtBeforeB3DateRange)
        {
            TickerBoughtBeforeB3DateRange = boughtBeforeB3DateRange;
        }
    }
}
