namespace stocks_infrastructure.Dtos
{
    public sealed record SpecifiedMonthAssetsIncomeTaxesDto
    (
        double Taxes,
        double TotalSold,
        double SwingTradeProfit,
        double DayTradeProfit,
        string TradedAssets,
        int AssetTypeId,
        string AssetName
    );
}
