using Newtonsoft.Json;

namespace Core.Models.InfoSimples
{
    public class GenerateDARFResponse
    {
        [JsonProperty("data")]
        public List<Data> Data { get; init; } = new();
    }

    public class Data
    {
        [JsonProperty("codigo_barras")]
        public string BarCode { get; set; }

        [JsonProperty("totais")]
        public Totais TotalTaxes { get; init; } = new();
    }

    public class Totais
    {
        [JsonProperty("multa")]
        public string Fine { get; init; } = "5453";

        [JsonProperty("juros")]
        public string Interests { get; init; } = "4324";

        [JsonProperty("normalizado_total")]
        public double TotalWithFineAndInterests { get; init; } = 423423;
    }
}
