using common.Helpers;
using Core.Models;
using Core.Refit.B3;
using Core.Services.B3ResponseCalculator;
using Infrastructure.Models;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.Taxes;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Hangfire.IncomeTaxesAdder
{
    public class IncomeTaxesAdderHangfire : IIncomeTaxesAdderHangfire
    {
        private readonly IB3ResponseCalculatorService b3ResponseCalculatorService;

        private readonly IAccountRepository accountRepository;
        private readonly IIncomeTaxesRepository incomeTaxesRepository;

        private readonly IB3Client b3Client;

        public readonly ILogger<IncomeTaxesAdderHangfire> logger;
        private const int DayToRunThisJob = 2;

        public IncomeTaxesAdderHangfire(IB3ResponseCalculatorService b3ResponseCalculatorService,
            IAccountRepository accountRepository,
            IIncomeTaxesRepository incomeTaxesRepository,
            IB3Client b3Client,
            ILogger<IncomeTaxesAdderHangfire> logger)
        {
            this.b3ResponseCalculatorService = b3ResponseCalculatorService;
            this.accountRepository = accountRepository;
            this.incomeTaxesRepository = incomeTaxesRepository;
            this.b3Client = b3Client;
            this.logger = logger;
        }

        public async Task Execute()
        {
            try
            {
                if (DateTime.UtcNow.AddHours(-3).Day != DayToRunThisJob) return;

                var accounts = await accountRepository.GetAll();

                foreach (var account in accounts)
                {
                    string lastMonth = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1).AddDays(-1).ToString("MM-yyyy");
                    var incomeTaxes = await incomeTaxesRepository.GetSpecifiedMonthTaxes(lastMonth, account.Id);

                    if (incomeTaxes.IsNullOrEmpty())
                    {
                        string startDate = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1).AddDays(-1).ToString("yyyy-MM-01");
                        string endDate = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1).AddDays(-1).ToString("yyyy-MM-dd");

                        var b3Response = await b3Client.GetAccountMovement(UtilsHelper.RemoveSpecialCharacters(account.CPF), startDate, endDate, account.Id);
                        var taxesResponse = await b3ResponseCalculatorService.Calculate(b3Response, account.Id);

                        if (taxesResponse is not null)
                            await SaveIncomeTaxes(taxesResponse, account);
                    }
                }
            } catch (Exception e)
            {
                logger.LogError(e, $"Ocorreu um erro no hangfire {nameof(IncomeTaxesAdderHangfire)}.");
            }
        }

        private async Task SaveIncomeTaxes(InvestorMovementDetails investorMovementDetails, Account account)
        {
            // TODO bulk insert
            foreach (var taxes in investorMovementDetails.Assets)
            {
                await incomeTaxesRepository.AddAsync(new IncomeTaxes(
                    taxes.Month,
                    taxes.Taxes,
                    taxes.TotalSold,
                    taxes.SwingTradeProfit,
                    taxes.DayTradeProfit,
                    JsonConvert.SerializeObject(taxes.TradedAssets),
                    account,
                    (int)taxes.AssetTypeId
                ));
            }            
        }
    }
}
