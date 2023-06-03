using Newtonsoft.Json;

namespace stocks_core.DTOs.B3
{
    public class Movement
    {
        public class Root
        {
            [JsonProperty("data")]
            public Data Data { get; set; }

            [JsonProperty("links")]
            public Links Links { get; set; }
        }

        public class Data
        {
            [JsonProperty("equitiesPeriods")]
            public EquitiesPeriods EquitiesPeriods { get; set; }
        }

        public class EquitiesPeriods
        {
            [JsonProperty("equitiesMovements")]
            public List<EquitMovement> EquitiesMovements { get; set; }
        }

        public class Links
        {
            [JsonProperty("self")]
            public string Self { get; set; }

            [JsonProperty("first")]
            public string First { get; set; }

            [JsonProperty("next")]
            public string? Next { get; set; }

            [JsonProperty("last")]
            public string Last { get; set; }
        }

        public class EquitMovement
        {
            public EquitMovement(
                string tickerSymbol,
                string corporationName,
                string assetType,
                string movementType,
                double operationValue,
                double quantity,
                double unitPrice,
                DateTime referenceDate,
                bool dayTraded = false
            )
            {
                TickerSymbol = tickerSymbol;
                CorporationName = corporationName;
                AssetType = assetType;
                MovementType = movementType;
                OperationValue = operationValue;
                EquitiesQuantity = quantity;
                UnitPrice = unitPrice;
                ReferenceDate = referenceDate;
                DayTraded = dayTraded;
            }

            public EquitMovement() {}

            // Obrigado por não retornarem ids, B3!
            [JsonIgnore]
            public Guid Id { get; } = Guid.NewGuid();

            [JsonIgnore]
            public bool DayTraded { get; set; } = false;


            [JsonProperty("referenceDate")]
            public DateTime ReferenceDate { get; set; }

            [JsonProperty("productTypeName")]
            public string AssetType { get; set; }

            [JsonProperty("movementType")]
            public string MovementType { get; set; }

            [JsonProperty("tickerSymbol")]
            public string TickerSymbol { get; set; }

            [JsonProperty("corporationName")]
            public string CorporationName { get; set; }

            [JsonProperty("operationValue")]
            public double OperationValue { get; set; }

            [JsonProperty("equitiesQuantity")]
            public double EquitiesQuantity { get; set; }

            [JsonProperty("unitPrice")]
            public double UnitPrice { get; set; }
        }
    }
}
