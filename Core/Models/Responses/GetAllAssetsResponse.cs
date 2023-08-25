namespace stocks_core.Models.Responses;

public sealed record GetAllAssetsResponse(
    string Ticker,
    double AveragePrice,
    int Quantity
);
