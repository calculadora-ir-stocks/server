using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Common.Configurations
{
    /// <summary>
    /// É utilizada como configuração em todos os JSONs de retorno da aplicação.
    /// </summary>
    public class JsonSerializerConfiguration
    {
        public JsonSerializerConfiguration()
        {
            settings = new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        private readonly JsonSerializerSettings settings;
        public JsonSerializerSettings Settings { get { return settings; } }
    }
}
