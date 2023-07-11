namespace stocks_infrastructure.Dtos
{
    public class SpecifiedMonthAssetsIncomeTaxesDto
    {
        public double Taxes { get; init; }
        public double TotalSold { get; init; }
        public double SwingTradeProfit { get; init; }
        public double DayTradeProfit { get; init; }
        public string TradedAssets { get; init; }
        public int AssetTypeId { get; init; }

    }
}
