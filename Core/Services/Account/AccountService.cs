﻿using common.Helpers;
using Common.Exceptions;
using Common.Options;
using Core.Models.Api.Requests.Account;
using Core.Refit.B3;
using Infrastructure.Models;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Services.Account
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;
        private readonly IB3Client b3Client;
        private readonly IOptions<B3ApiOptions> b3Options;
        private readonly ILogger<AccountService> logger;

        public AccountService(IAccountRepository accountRepository, IAverageTradedPriceRepostory averageTradedPriceRepository, 
            IB3Client b3Client, IOptions<B3ApiOptions> b3Options, ILogger<AccountService> logger)
        {
            this.accountRepository = accountRepository;
            this.averageTradedPriceRepository = averageTradedPriceRepository;
            this.b3Client = b3Client;
            this.b3Options = b3Options;
            this.logger = logger;
        }

        public async Task<bool> Delete(Guid accountId)
        {
            try
            {
                var account = await accountRepository.GetById(accountId) ?? throw new NotFoundException("Investidor", accountId.ToString());
                accountRepository.Delete(account);
                logger.LogInformation("O usuário de id {accountId} deletou a sua conta da plataforma.", accountId);

                var response = await b3Client.OptOut(UtilsHelper.RemoveSpecialCharacters(account.CPF));
                return response.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao deletar a conta do usuário de id {id}.", accountId);
                throw;
            }
        }

        public async Task<Guid> GetByAuth0Id(string auth0Id)
        {
            return await accountRepository.GetByAuth0IdAsNoTracking(auth0Id);
        }

        public string GetOptInLink()
        {
            string link = $"https://b3investidor.b2clogin.com/b3investidor.onmicrosoft.com/oauth2/" +
                $"v2.0/authorize?p=B2C_1A_FINTECH&client_id={b3Options.Value.ClientId}&nonce=defaultNonce&redirect_uri=" +
                $"https%3A%2F%2Fwww.investidor.b3.com.br&scope=openid&response_type=code&prompt=login";
            return link;
        }

        public async Task<bool> OptIn(Guid accountId)
        {
            var account = await accountRepository.GetById(accountId) ?? throw new NotFoundException("Investidor", accountId.ToString());
            return await b3Client.OptIn(UtilsHelper.RemoveSpecialCharacters(account.CPF));
        }

        public async Task SetupAverageTradedPrices(SetupAverageTradedPriceRequest request, Guid accountId)
        {
            var account = await accountRepository.GetById(accountId);
            if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

            // TODO bulk insert
            foreach (var ticker in request.AverageTradedPrices)
            {
                AverageTradedPrice averageTradedPrice = new(ticker.Ticker, ticker.AveragePrice, ticker.TotalBought, ticker.Quantity, account, DateTime.Now);
                await averageTradedPriceRepository.AddAsync(averageTradedPrice);
            }
        }
    }
}
