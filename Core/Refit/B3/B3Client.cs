using Api.DTOs.Auth;
using Azure;
using common.Helpers;
using Common.Options;
using Core.Models.B3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;
using System.Diagnostics;
using System.Net;
using static System.Net.WebRequestMethods;

namespace Core.Refit.B3
{
    public class B3Client : IB3Client
    {
        private readonly IOptions<B3ApiOptions> options;

        private static B3Token? token = null;
        private readonly ILogger<B3Client> logger;

        public B3Client(IOptions<B3ApiOptions> options, ILogger<B3Client> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task<HttpStatusCode> B3HealthCheck()
        {
            //string b3Token = await GetOrGenerateAuthToken();
            //var response = await b3Client.B3HealthCheck(b3Token);

            return HttpStatusCode.OK;
        }        

        public async Task<Movement.Root?> GetAccountMovement(string cpf, string referenceStartDate, string referenceEndDate, Guid accountId)
        {
            return new Movement.Root();
        }

        /// <summary>
        /// Como a API da B3 separa o response por páginas, é necessário percorrê-las
        /// afim de obter todos os dados.
        /// </summary>
        private async Task GetAccountMovementsInAllPages(Movement.Root root)
        {
            
        }

        public async Task<bool> OptIn(string cpf)
        {
            return true;
        }

        public async Task<ApiResponse<object>> OptOut(string cpf)
        {
            return new ApiResponse<object>(null, null, null);
        }

        private async Task<string> GetOrGenerateAuthToken()
        {
            if (token is null || token.Expired)
                return await GenerateAuthToken();
            else
                return token.AccessToken;
        }

        private async Task<string> GenerateAuthToken()
        {
            return string.Empty;
        }
    }
}
