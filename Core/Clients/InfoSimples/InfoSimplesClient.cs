using Core.Models.InfoSimples;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Core.Clients.InfoSimples
{
    public class InfoSimplesClient : IInfoSimplesClient
    {
        private readonly IHttpClientFactory httpClient;
        private readonly HttpClient client;

        private readonly ILogger<InfoSimplesClient> logger;

        // TODO get token from appsettings.json
        private const string Token = "ys0RvnIznomL-J8f0_OUEj06OKFIE1BZnxk2xpLz";

        public InfoSimplesClient(IHttpClientFactory httpClient, ILogger<InfoSimplesClient> logger)
        {
            this.httpClient = httpClient;
            client = this.httpClient.CreateClient("Infosimples");

            this.logger = logger;
        }

        public async Task<GenerateDARFResponse> GenerateDARF(GenerateDARFRequest request)
        {
            logger.LogInformation("Iniciando geração de DARF.");

            HttpRequestMessage infoSimplesRequest = 
                new(HttpMethod.Get,
                $"receita-federal/sicalc/darf?token={Token}&cpf={request.CPF}&birthdate={request.BirthDate}" +
                $"&observacoes={request.Observacoes}&codigo={request.Codigo}&valor_principal={request.ValorPrincipal}" +
                $"&periodo_apuracao={request.PeriodoApuracao}&data_consolidacao={request.DataConsolidacao}"
            );

            using var response = await client.SendAsync(infoSimplesRequest, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var responseContentStream = await response.Content.ReadAsStringAsync();

            var darf = JsonConvert.DeserializeObject<GenerateDARFResponse>(responseContentStream);

            if (darf is null) throw new Exception("Ocorreu um erro ao gerar a DARF.");

            return darf;
        }
    }
}
