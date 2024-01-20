using Common.Constants;
using Common.Enums;
using Common.Helpers;
using DevOne.Security.Cryptography.BCrypt;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Account : BaseEntity
    {
        public Account(string auth0Id,
            string cpf,
            string birthDate,
            string phoneNumber
        )
        {
            Auth0Id = auth0Id;
            CPF = cpf;
            BirthDate = birthDate;
            PhoneNumber = phoneNumber;

            Plan = new Plan(PlansConstants.Free, accountId: Id, account: this, DateTime.Now.AddMonths(1));

            Validate(this, new AccountValidator());
        }

        public Account() { }

        /// <summary>
        /// A propriedade <c>sub</c> retornada pelo JWT do Auth0. É utilizado como ID para consultar
        /// as informações do usuário.
        /// </summary>
        public string Auth0Id { get; init; }

        #region Registration information
        /// <summary>
        /// CPF formatado (111.111.111-11) de uma conta cadastrada.
        /// É utilizado principalmente para fazer o vínculo com a API da B3.
        /// </summary>
        public string CPF { get; init; }

        /// <summary>
        /// Data de nascimento formatada (dd/MM/yyyy) de um investidor.
        /// </summary>
        public string BirthDate { get; init; }

        /// <summary>
        /// Telefone pessoal formatado (+11 11 9 1111-1111) de uma conta cadastrada.
        /// </summary>
        public string PhoneNumber { get; init; }

        /// <summary>
        /// O id do objeto <c>Customer</c> criado pelo Stripe no registro de uma conta.
        /// O objeto <c>Customer</c> do Stripe é utilizado para vincular pagamentos já existentes
        /// a um usuário.
        /// Para ler mais, acesse a <a
        /// href="https://stripe.com/docs/api/checkout/sessions/create#create_checkout_session-payment_intent_data-setup_future_usage">documentação oficial</a>.
        /// </summary>
        public string? StripeCustomerId { get; set; } = null;

        /// <summary>
        /// Define os status de uma conta cadastrada.
        /// Quando um usuário é registrado, o status definido é <c>EmailNotConfirmed</c>.
        /// </summary>
        public string Status { get; set; } = EnumHelper.GetEnumDescription(AccountStatus.EmailNotConfirmed);
        #endregion

        #region Relationships
        /// <summary>
        /// Um investidor possui diversos tickers com preços médios.
        /// </summary>
        public ICollection<AverageTradedPrice>? AverageTradedPrices { get; protected set; }

        /// <summary>
        /// Um investidor possui diversos meses com pagamentos de impostos pendentes.
        /// </summary>
        public ICollection<IncomeTaxes>? IncomeTaxes { get; set; }

        /// <summary>
        /// Um investidor possui um único código de confirmação de e-mail por vez.
        /// Código enviado para o e-mail para confirmação.
        /// Pode ser um código de autenticação ou um código para alteração de senha.
        /// </summary>
        public EmailCode EmailCode { get; set; }

        /// <summary>
        /// Um investidor possui um único plano ativo por vez.
        /// </summary>
        public Plan Plan { get; set; }
        #endregion
    }

    public partial class AccountValidator : AbstractValidator<Account>
    {
        private readonly Regex HasNumber = new("[0-9]+");
        private readonly Regex HasUpperChar = new("[A-Z]+");
        private readonly Regex HasLowerChar = new("[a-z]+");
        private readonly Regex HasMinMaxChars = new(".{8}");

        private readonly Regex IsValidEmail = new(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        private readonly Regex IsValidCPF = new(@"(^\d{3}\.\d{3}\.\d{3}\-\d{2}$)");
        private readonly Regex IsValidPhoneNumber = new(@"^\+\d{2} \d{2} \d \d{4}-\d{4}$");
        private readonly Regex IsValidBirthDate = new(@"^\d{2}/\d{2}/\d{4}$");

        private const int NameMinLength = 8;

        public AccountValidator()
        {
            RuleFor(c => c.CPF)
                .Must(c => IsValidCPF.IsMatch(c.ToString()))
                .WithMessage($"O CPF informado não é válido.");

            RuleFor(c => c.PhoneNumber)
                .Must(c => IsValidPhoneNumber.IsMatch(c.ToString()))
                .WithMessage($"Número de telefone deve seguir o formato +11 11 1 1111-1111");

            RuleFor(c => c.BirthDate)
                .Must(c => IsValidBirthDate.IsMatch(c.ToString()))
                .WithMessage($"A data de nascimento deve seguir o formato dd/MM/yyyy");
        }
    }
}
