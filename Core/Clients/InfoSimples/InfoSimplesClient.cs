﻿using Common.Options;
using Core.Models.InfoSimples;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Web;

namespace Core.Clients.InfoSimples
{
    public class InfoSimplesClient : IInfoSimplesClient
    {
        private readonly IHttpClientFactory httpClient;
        private readonly HttpClient client;

        private readonly IOptions<InfoSimplesOptions> secret;
        private readonly ILogger<InfoSimplesClient> logger;

        public InfoSimplesClient(IHttpClientFactory httpClient, IOptions<InfoSimplesOptions> secret, ILogger<InfoSimplesClient> logger)
        {
            this.httpClient = httpClient;

            client = this.httpClient.CreateClient("Infosimples");
            client.Timeout = TimeSpan.FromMinutes(10);

            this.secret = secret;
            this.logger = logger;
        }

        public async Task<GenerateDARFResponse> GenerateDARF(GenerateDARFRequest request)
        {
            logger.LogInformation("Iniciando geração de DARF.");

            string encodedUrl =
                    $"receita-federal/sicalc/darf?token={secret.Value.ApiToken}&cnpj=&cpf={request.CPF}&birthdate={HttpUtility.UrlEncode(request.BirthDate)}" +
                    $"&observacoes={HttpUtility.UrlEncode(request.Observacoes)}&codigo={request.Codigo}" +
                    $"&valor_principal={request.ValorPrincipal.ToString().Replace(",", ".")}" +
                    $"&periodo_apuracao={HttpUtility.UrlEncode(request.PeriodoApuracao)}&data_consolidacao={HttpUtility.UrlEncode(request.DataConsolidacao)}" +
                    $"&numero_referencia=&quota=";

            HttpRequestMessage infoSimplesRequest = new(HttpMethod.Get, encodedUrl);

            using var response = await client.SendAsync(infoSimplesRequest, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var responseContentStream = await response.Content.ReadAsStringAsync();

            var darf = JsonConvert.DeserializeObject<GenerateDARFResponse>(responseContentStream)!;

            if (darf.Data is null) throw new Exception("Não foi possível gerar a DARF para esse mês.");

            return darf;
        }
    }
}
