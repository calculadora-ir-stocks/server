namespace Core.Models
{
    public class CalculateProfitResponse
    {
        public CalculateProfitResponse()
        {
            DayTradeOperations = new();
            SwingTradeOperations = new();
            OperationHistory = new();
            TickersBoughtBeforeB3Range = new();
            TickerStatusAtTheEndOfTheYear = new();
        }

        public List<MovementProperties> DayTradeOperations { get; init; }
        public List<MovementProperties> SwingTradeOperations { get; init; }
        public List<OperationDetails> OperationHistory { get; init; }
        public List<string> TickersBoughtBeforeB3Range { get; init; }
        public List<AverageTradedPriceDetails> TickerStatusAtTheEndOfTheYear { get; init; }
    }
}
