using Newtonsoft.Json;

namespace Core.Models
{
    public class MovementProperties
    {
        public MovementProperties(string tickerSymbol, bool tickerBoughtBeforeB3DateRange = false)
        {
            TickerSymbol = tickerSymbol;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public string TickerSymbol { get; init; }
        public double Profit { get; set; } = 0;

        [JsonIgnore]
        public bool TickerBoughtBeforeB3DateRange { get; set; }
    }
}
