using Newtonsoft.Json;

namespace Core.Models.InfoSimples
{
    public class GenerateDARFResponse
    {
        [JsonProperty("data")]
        public List<Data> Data { get; init; }
    }

    public class Data
    {
        [JsonProperty("codigo_barras")]
        public string CodigoDeBarras { get; init; }

        [JsonProperty("totais")]
        public Totais Totais { get; init; }
    }

    public class Totais
    {
        [JsonProperty("multa")]
        public string Multa { get; init; }

        [JsonProperty("juros")]
        public string Juros { get; init; }

        [JsonProperty("normalizado_total")]
        public double NormalizadoTotal { get; init; }
    }
}
