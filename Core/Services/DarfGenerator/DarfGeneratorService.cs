﻿using Common.Enums;
using common.Helpers;
using Common.Helpers;
using Core.Clients.InfoSimples;
using Core.Models.InfoSimples;
using Core.Models.Responses;
using Infrastructure.Repositories.Taxes;
using Infrastructure.Repositories;
using Common.Exceptions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Server.IIS.Core;

namespace Core.Services.DarfGenerator
{
    public class DarfGeneratorService : IDarfGeneratorService
    {
        private readonly IGenericRepository<Infrastructure.Models.Account> genericRepositoryAccount;
        private readonly IIncomeTaxesRepository taxesRepository;
        private readonly IInfoSimplesClient infoSimplesClient;
        private const string DarfCode = "6015-01";

        public DarfGeneratorService(
            IGenericRepository<Infrastructure.Models.Account> genericRepositoryAccount,
            IIncomeTaxesRepository taxesRepository,
            IInfoSimplesClient infoSimplesClient
        )
        {
            this.genericRepositoryAccount = genericRepositoryAccount;
            this.taxesRepository = taxesRepository;
            this.infoSimplesClient = infoSimplesClient;
        }

        public async Task<DARFResponse> Generate(Guid accountId, string month, double value = 0)
        {
            // TODO tá pegando o CPF criptografado
            var account = await genericRepositoryAccount.GetByIdAsync(accountId);
            if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

            if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired))
                throw new ForbiddenException("O plano do usuário está expirado.");

            var taxes = await taxesRepository.GetSpecifiedMonthTaxes(month, accountId);
            double totalTaxes = taxes.Select(x => x.Taxes).Sum();

            totalTaxes += value;

            if (taxes.IsNullOrEmpty() || totalTaxes <= 0)
                throw new NotFoundException("Nenhum imposto foi encontrado para esse mês, logo, a DARF não pode ser gerada.");

            string taxesReferenceDate = taxes.Select(x => x.Month).First();
            string today = DateTime.Now.ToString("dd/MM/yyyy");

            // TODO uncomment for production

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

            // string? observation = null;

            // if (response.Data[0].TotalTaxes.TotalWithFineAndInterests < 10)
            // {
            //     observation = "Valor total da DARF é inferior ao valor mínimo de R$10,00. \n" +
            //         "Para pagá-la, adicione esse imposto em algum mês subsequente até que o valor total seja igual ou maior que R$10,00.";
            // }

            var monthsToCompensate = await taxesRepository.GetTaxesLessThanMinimumRequired(accountId, month);

            return new DARFResponse(
                "423454257291",
                totalTaxes,
                0,
                7,
                string.Empty,
                monthsToCompensate
            );

            // TODO uncomment for production

            // return new DARFResponse(
            //     response.Data[0].BarCode,
            //     response.Data[0].TotalTaxes.TotalWithFineAndInterests,
            //     double.Parse(response.Data[0].TotalTaxes.Fine),
            //     double.Parse(response.Data[0].TotalTaxes.Interests),
            //     observation,
            //     monthsToCompensate
            // );
        }
    }
}
