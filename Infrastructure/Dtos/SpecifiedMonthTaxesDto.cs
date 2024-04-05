using Common.Enums;

namespace Infrastructure.Dtos
{
    public record SpecifiedMonthTaxesDto
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SpecifiedMonthTaxesDto()
        {
        }

        public string Month { get; init; }
        public double Taxes { get; init; }
        public bool Paid { get; init; }
        public string TradedAssets { get; init; }
        public IEnumerable<SpecifiedMonthTaxesDtoDetails> SerializedTradedAssets { get; set; }
    }

    public class SpecifiedMonthTaxesDtoDetails
    {
        public int Day { get; set; }
        public string DayOfTheWeek { get; set; }

        /// <summary>
        /// O nome do tipo de ativo sendo negociado.
        /// </summary>
        public string AssetType { get; set; }

        public Asset AssetTypeId { get; init; }

        public string TickerSymbol { get; set; }

        public string MovementType { get; set; }

        public int Quantity { get; set; }

        public double Total { get; set; }
    }
}
