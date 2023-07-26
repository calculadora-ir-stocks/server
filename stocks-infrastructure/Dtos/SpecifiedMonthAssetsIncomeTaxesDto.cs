namespace stocks_infrastructure.Dtos
{
    public record SpecifiedMonthAssetsIncomeTaxesDto
    {
        public SpecifiedMonthAssetsIncomeTaxesDto(double taxes, double totalSold, double swingTradeProfit, double dayTradeProfit, string tradedAssets, int assetTypeId, string assetName)
        {
            Taxes = taxes;
            TotalSold = totalSold;
            SwingTradeProfit = swingTradeProfit;
            DayTradeProfit = dayTradeProfit;
            TradedAssets = tradedAssets;
            AssetTypeId = assetTypeId;
            AssetName = assetName;
        }

        public double Taxes { get; init; }
        public double TotalSold { get; init; }
        public double SwingTradeProfit { get; init; }
        public double DayTradeProfit { get; init; }
        public string TradedAssets { get; init; }
        public int AssetTypeId { get; init; }
        public string AssetName { get; init; }
    }
}
