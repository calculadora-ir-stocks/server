using Common.Enums;

namespace Infrastructure.Dtos
{
    public record SpecifiedMonthTaxesDto
    {
        public SpecifiedMonthTaxesDto(
            double taxes,
            string month,
            IEnumerable<SpecifiedMonthTaxesDtoDetails> details
        )
        {
            Taxes = taxes;
            Month = month;
            TradedAssets = details;
        }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SpecifiedMonthTaxesDto() { }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public string Month { get; init; }
        public double Taxes { get; init; }        
        public IEnumerable<SpecifiedMonthTaxesDtoDetails> TradedAssets { get; init; }
    }

    public class SpecifiedMonthTaxesDtoDetails
    {
        public SpecifiedMonthTaxesDtoDetails(
            int day,
            string dayOfTheWeek,
            string assetType,
            Asset assetTypeId,
            string tickerSymbol,
            string movementType,
            int quantity,
            double value
        )
        {
            Day = day;
            DayOfTheWeek = dayOfTheWeek;
            AssetType = assetType;
            AssetTypeId = assetTypeId;
            TickerSymbol = tickerSymbol;
            MovementType = movementType;
            Quantity = quantity;
            Total = value;
        }

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
