using stocks_infrastructure.Models;

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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AverageTradedPriceDto() { }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public string Ticker { get; init; }
        public double AverageTradedPrice { get; init; }
        public int Quantity { get; init; }
    }
}
