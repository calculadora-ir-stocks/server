namespace stocks_infrastructure.Dtos
{
    public record AverageTradedPriceDto
    {
        public AverageTradedPriceDto(string ticker, double averageTradedPrice, int quantity)
        {
            Ticker = ticker;
            AverageTradedPrice = averageTradedPrice;
            Quantity = quantity;
        }

        public string Ticker { get; init; }
        public double AverageTradedPrice { get; init; }
        public int Quantity { get; init; }
    }
}
