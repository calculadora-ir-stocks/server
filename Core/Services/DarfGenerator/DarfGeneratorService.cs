using common.Helpers;
using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Core.Clients.InfoSimples;
using Core.Models.InfoSimples;
using Core.Models.Responses;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.Taxes;
using Microsoft.IdentityModel.Tokens;

namespace Core.Services.DarfGenerator
{
    public class DarfGeneratorService : IDarfGeneratorService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IIncomeTaxesRepository taxesRepository;
        private readonly IInfoSimplesClient infoSimplesClient;
        private const string DarfCode = "6015-01";

        public DarfGeneratorService(
            IAccountRepository accountRepository,
            IIncomeTaxesRepository taxesRepository,
            IInfoSimplesClient infoSimplesClient
        )
        {
            this.accountRepository = accountRepository;
            this.taxesRepository = taxesRepository;
            this.infoSimplesClient = infoSimplesClient;
        }

        public async Task<DARFResponse> Generate(Guid accountId, string month, double value = 0)
        {
            var account = await accountRepository.GetById(accountId) ?? throw new NotFoundException("Investidor", accountId.ToString());

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
                throw new ForbiddenException("O plano do usuário está expirado.");

            var taxes = await taxesRepository.GetSpecifiedMonthTaxes(month, accountId);
            double totalTaxes = taxes.Select(x => x.Taxes).Sum();

            totalTaxes += value;

            if (taxes.IsNullOrEmpty() || totalTaxes <= 0)
                throw new NotFoundException("Nenhum imposto foi encontrado para esse mês, logo, a DARF não pode ser gerada.");

            string taxesReferenceDate = taxes.Select(x => x.Month).First();
            string today = DateTime.UtcNow.AddHours(-3).ToString("dd/MM/yyyy");

            var response = await infoSimplesClient.GenerateDARF(
                new GenerateDARFRequest
                (
                    UtilsHelper.RemoveSpecialCharacters(account.CPF),
                    account.BirthDate,
                    $"Venda de ativos no mês {taxesReferenceDate}. Essa DARF foi gerada automaticamente " +
                    $"pelo Stocks IR em {today}.",
                    DarfCode,
                    totalTaxes,
                    taxesReferenceDate,
                    today
                )
            );

            string? observation = null;

            if (response.Data[0].TotalWithFineAndInterests < 10)
            {
                observation = "Valor total da DARF é inferior ao valor mínimo de R$10,00. \n" +
                    "Para pagá-la, adicione esse imposto em algum mês subsequente até que o valor total seja igual ou maior que R$10,00.";
            }

            var monthsToCompensate = await taxesRepository.GetTaxesLessThanMinimumRequired(accountId, month);

            return new DARFResponse(
                response.Data[0].BarCode,
                response.Data[0].TotalWithFineAndInterests,
                double.Parse(response.Data[0].Fine),
                double.Parse(response.Data[0].Interests),
                observation,
                monthsToCompensate
            );
        }
    }
}
