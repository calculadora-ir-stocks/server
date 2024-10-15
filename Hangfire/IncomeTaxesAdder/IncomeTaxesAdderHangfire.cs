using common.Helpers;
using Core.Refit.B3;
using Core.Services.B3ResponseCalculator;
using Infrastructure.Models;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.Taxes;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using static Core.Models.B3.Movement;

namespace Hangfire.IncomeTaxesAdder
{
    public class IncomeTaxesAdderHangfire : IIncomeTaxesAdderHangfire
    {
        private readonly IB3ResponseCalculatorService b3ResponseCalculatorService;

        private readonly IAccountRepository accountRepository;
        private readonly IIncomeTaxesRepository incomeTaxesRepository;
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

        private readonly IB3Client b3Client;

        public readonly ILogger<IncomeTaxesAdderHangfire> logger;
        private const int DayToRunThisJob = 2;

        public IncomeTaxesAdderHangfire(IB3ResponseCalculatorService b3ResponseCalculatorService,
            IAccountRepository accountRepository,
            IIncomeTaxesRepository incomeTaxesRepository,
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IB3Client b3Client,
            ILogger<IncomeTaxesAdderHangfire> logger)
        {
            this.b3ResponseCalculatorService = b3ResponseCalculatorService;
            this.accountRepository = accountRepository;
            this.incomeTaxesRepository = incomeTaxesRepository;
            this.averageTradedPriceRepository = averageTradedPriceRepository;
            this.b3Client = b3Client;
            this.logger = logger;
        }

        public async Task Execute()
        {
            try
            {
                if (new CustomDateTime().UtcNow.AddHours(-3).Day != DayToRunThisJob) return;

                var accounts = await accountRepository.GetAll();

                foreach (var account in accounts)
                {
                    string lastMonth = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1).AddDays(-1).ToString("MM/yyyy");
                    var incomeTaxes = await incomeTaxesRepository.GetSpecifiedMonthTaxes(lastMonth, account.Id);

                    if (incomeTaxes.IsNullOrEmpty())
                    {
                        string startDate = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1).AddDays(-1).ToString("yyyy-MM-01");
                        string endDate = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1).AddDays(-1).ToString("yyyy-MM-dd");

                        var b3Response = await b3Client.GetAccountMovement(UtilsHelper.RemoveSpecialCharacters(account.CPF), startDate, endDate, account.Id);

                        if (b3Response is not null)
                        {
                            await SaveIncomeTaxes(b3Response, account);
                        }
                    }
                }
            } catch (Exception e)
            {
                logger.LogError(e, $"Ocorreu um erro no hangfire {nameof(IncomeTaxesAdderHangfire)}.");
            }
        }

        private async Task SaveIncomeTaxes(Root b3Response, Account account)
        {
            var taxesResponse = await b3ResponseCalculatorService.Calculate(b3Response, account.Id);

            if (taxesResponse is not null)
            {
                foreach (var taxes in taxesResponse.Assets)
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

    // Gambiarra pra conseguir mockar o DateTime.UtcNow.
    public class CustomDateTime
    {
        public CustomDateTime()
        {
        }

        public virtual DateTime UtcNow { get; } = DateTime.UtcNow;
    }
}
