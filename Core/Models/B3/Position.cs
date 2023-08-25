using Newtonsoft.Json;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Core.DTOs.B3
{
    public class Position
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
            [JsonProperty("equitiesPositions")]
            public List<EquitiesPosition> EquitiesPositions { get; set; }
        }

        public class EquitiesPosition
        {
            [JsonProperty("documentNumber")]
            public string DocumentNumber { get; set; }

            [JsonProperty("referenceDate")]
            public string ReferenceDate { get; set; }

            [JsonProperty("productCategoryName")]
            public string ProductCategoryName { get; set; }

            [JsonProperty("productTypeName")]
            public string ProductTypeName { get; set; }

            [JsonProperty("markingIndicator")]
            public bool MarkingIndicator { get; set; }

            [JsonProperty("tickerSymbol")]
            public string TickerSymbol { get; set; }

            [JsonProperty("participantName")]
            public string ParticipantName { get; set; }

            [JsonProperty("specificationCode")]
            public string SpecificationCode { get; set; }

            [JsonProperty("corporationName")]
            public string CorporationName { get; set; }

            [JsonProperty("participantDocumentNumber")]
            public string ParticipantDocumentNumber { get; set; }

            [JsonProperty("equitiesQuantity")]
            public int EquitiesQuantity { get; set; }

            [JsonProperty("closingPrice")]
            public int ClosingPrice { get; set; }

            [JsonProperty("updateValue")]
            public int UpdateValue { get; set; }

            [JsonProperty("isin")]
            public string Isin { get; set; }

            [JsonProperty("distributionIdentification")]
            public string DistributionIdentification { get; set; }

            [JsonProperty("bookkeeperName")]
            public string BookKeeperName { get; set; }

            [JsonProperty("availableQuantity")]
            public int AvailableQuantity { get; set; }

            [JsonProperty("unavailableQuantity")]
            public int UnavailableQuantity { get; set; }

            [JsonProperty("administratorName")]
            public string AdministratorName { get; set; }

            [JsonProperty("reasons")]
            public List<Reason> Reasons { get; set; }
        }

        public class Links
        {
            [JsonProperty("self")]
            public string Self { get; set; }

            [JsonProperty("first")]
            public string First { get; set; }

            [JsonProperty("prev")]
            public string Prev { get; set; }

            [JsonProperty("next")]
            public string? Next { get; set; }

            [JsonProperty("last")]
            public string Last { get; set; }
        }

        public class Reason
        {
            [JsonProperty("reasonName")]
            public string ReasonName { get; set; }

            [JsonProperty("collateralQuantity")]
            public int CollateralQuantity { get; set; }

            [JsonProperty("counterpartName")]
            public string CounterpartName { get; set; }
        }
    }
}
