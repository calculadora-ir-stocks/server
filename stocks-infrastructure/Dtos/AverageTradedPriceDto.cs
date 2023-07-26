namespace stocks_infrastructure.Dtos
{
    public sealed record AverageTradedPriceDto
    (
        string Ticker,
        double AverageTradedPrice,
        int Quantity
    );
}
