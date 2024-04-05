namespace Infrastructure.Dtos
{
    public record AverageTradedPriceDto
    {
        public AverageTradedPriceDto(string ticker, double averageTradedPrice, double totalBought, int quantity, Guid accountId)
        {
            Ticker = ticker;
            AverageTradedPrice = averageTradedPrice;
            TotalBought = totalBought;
            Quantity = quantity;
            AccountId = accountId;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AverageTradedPriceDto() { }

        public string Ticker { get; init; }
        public double AverageTradedPrice { get; init; }
        public double TotalBought { get; init; }
        public int Quantity { get; init; }
        public Guid AccountId { get; init; }
    }
}
