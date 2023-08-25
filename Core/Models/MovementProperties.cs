using Newtonsoft.Json;

namespace stocks_core.Models
{
    public class MovementProperties
    {
        public MovementProperties(string tickerSymbol, bool tickerBoughtBeforeB3DateRange = false)
        {
            TickerSymbol = tickerSymbol;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public string TickerSymbol { get; init; }
        public double Profit { get; protected set; } = 0;

        [JsonIgnore]
        public bool TickerBoughtBeforeB3DateRange { get; protected set; }

        public void UpdateProfit(double profit)
        {
            Profit = profit;
        }

        public void UpdateTickerBoughtBeforeB3DateRange(bool boughtBeforeB3DateRange)
        {
            TickerBoughtBeforeB3DateRange = boughtBeforeB3DateRange;
        }
    }
}
