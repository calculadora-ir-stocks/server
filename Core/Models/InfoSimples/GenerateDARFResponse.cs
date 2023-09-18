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
    }

}
