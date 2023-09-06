namespace Core.Models
{
    public class CalculateProfitResponse
    {
        public CalculateProfitResponse(
        )
        {
            DayTradeOperations = new();
            SwingTradeOperations = new();
            OperationHistory = new();
        }

        public List<MovementProperties> DayTradeOperations { get; init; }
        public List<MovementProperties> SwingTradeOperations { get; init; }

        /// <summary>
        /// O histórico de operações realizadas.
        /// </summary>
        public List<OperationDetails> OperationHistory { get; init; }
    }
}
