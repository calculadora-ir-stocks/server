using Common.Enums;

namespace Core.Models.Api.Responses
{
    /// <summary>
    /// Response utilizado na tela principal do Stocks.
    /// </summary>
    public class TaxesDetailsResponse
    {
        public TaxesDetailsResponse(double totalTaxes, TaxesStatus status, string year)
        {
            TotalTaxes = totalTaxes;
            Status = status;
            Year = year;
            Movements = new();
        }


        /// <summary>
        /// O total de imposto a ser pago (soma dos impostos de todos os ativos de um determinado mês).
        /// </summary>
        public double TotalTaxes { get; set; }

        public TaxesStatus Status { get; init; }

        public string Year { get; init; }

        public List<Movement> Movements { get; set; }
    }

    public class Movement
    {
        public Movement(string dayOfTheWeek, IEnumerable<Details> tradedAssets)
        {
            DayOfTheWeek = dayOfTheWeek;
            TradedAssets = tradedAssets;
        }

        public string DayOfTheWeek { get; set; }
        public IEnumerable<Details> TradedAssets { get; set; }
    }

    public class Details
    {
        public Details(Asset assetTypeId, string assetTypeName, string movementType, string ticker, double total, int quantity)
        {
            AssetTypeId = assetTypeId;
            AssetTypeName = assetTypeName;
            MovementType = movementType;
            Ticker = ticker;
            Total = total;
            Quantity = quantity;
        }

        /// <summary>
        /// O id do tipo de ativo sendo negociado.
        /// </summary>
        public Asset AssetTypeId { get; set; }

        /// <summary>
        /// O nome do tipo de ativo sendo negociado.
        /// </summary>
        public string AssetTypeName { get; set; }

        public string MovementType { get; set; }

        public string Ticker { get; set; }

        public double Total { get; set; }

        public int Quantity { get; set; }
    }
}
