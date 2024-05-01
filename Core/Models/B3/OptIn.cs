using Newtonsoft.Json;

namespace Core.Models.B3
{
    public class OptIn
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

    }
    public class Data
    {
        [JsonProperty("authorizationIndicator")]
        public bool Authorized { get; set; }
    }
}
