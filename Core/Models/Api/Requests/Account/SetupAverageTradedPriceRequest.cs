namespace Core.Models.Api.Requests.Account
{
    public class SetupAverageTradedPriceRequest
    {
        public IEnumerable<AverageTradedPriceRequest> AverageTradedPrices { get; init; }
    }

    public record AverageTradedPriceRequest(string Ticker, double AveragePrice, double TotalBought, int Quantity);
}
