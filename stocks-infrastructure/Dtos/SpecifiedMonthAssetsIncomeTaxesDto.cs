﻿using stocks_common.Models;

namespace stocks_infrastructure.Dtos
{
    public class SpecifiedMonthAssetsIncomeTaxesDto
    {
        public double Taxes { get; init; }
        public double TotalSold { get; init; }
        public double SwingTradeProfit { get; init; }
        public double DayTradeProfit { get; init; }
        public IEnumerable<OperationDetails> TradedAssets { get; init; }
        public int AssetTypeId { get; init; }
        public string AssetName { get; init; }
    }
}
