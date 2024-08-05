using Newtonsoft.Json;

namespace Core.Models.InfoSimples
{
    public class GenerateDARFResponse
    {
        [JsonProperty("data")]
        public List<Data> Data { get; set; } = new();
    }

    public class Data
    {
        [JsonProperty("codigo_barras")]
        public string BarCode { get; set; }

        [JsonProperty("normalizado_valor_multa")]
        public string Fine { get; set; }

        [JsonProperty("normalizado_valor_juros_encargos")]
        public string Interests { get; set; }

        [JsonProperty("normalizado_valor_total")]
        public double TotalWithFineAndInterests { get; set; }
    }

    public class Totais
    {
        [JsonProperty("normalizado_valor_total")]
        public double TotalWithFineAndInterests { get; set; }
    }
}
