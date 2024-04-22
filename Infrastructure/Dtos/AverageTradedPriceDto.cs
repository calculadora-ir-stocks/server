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

        public string Ticker { get; set; }
        public double AverageTradedPrice { get; set; }
        public double TotalBought { get; set; }
        public int Quantity { get; set; }
        public Guid AccountId { get; set; }
    }
}
