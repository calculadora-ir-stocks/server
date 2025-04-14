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
            LastTimeOperatedInTimeInterval = new();
        }

        public List<MovementProperties> DayTradeOperations { get; init; }
        public List<MovementProperties> SwingTradeOperations { get; init; }
        public List<OperationDetails> OperationHistory { get; init; }
        public List<string> TickersBoughtBeforeB3Range { get; init; }

        /// <summary>
        /// Contém a última vez que um ticker foi operado.
        /// </summary>
        public List<AverageTradedPriceDetails> LastTimeOperatedInTimeInterval { get; init; }
    }
}
