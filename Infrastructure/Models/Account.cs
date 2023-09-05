using DevOne.Security.Cryptography.BCrypt;
using FluentValidation;
using Common.Constants;
using System.Text.RegularExpressions;

namespace Infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Account : BaseEntity
    {
        public Account(string name, string email, string password, string cpf)
        {
            Name = name;
            Email = email;
            Password = BCryptHelper.HashPassword(password, BCryptHelper.GenerateSalt());
            CPF = cpf;

            Validate(this, new AccountValidator());
        }

        public Account() { }

        #region Registration information
        /// <summary>
        /// Nome completo de uma conta cadastrada.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Endereço de e-mail válido de uma conta cadastrada.
        /// </summary>
        public string Email { get; init; }

        /// <summary>
        /// Senha de uma conta cadastrada.
        /// Deve conter no mínimo uma letra maiúscula, um número, um caractere especial e 8 dígitos.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// CPF formatado (111.111.111-11) de uma conta cadastrada.
        /// É utilizado principalmente para fazer o vínculo com a API da B3.
        /// </summary>
        public string CPF { get; init; }

        /// <summary>
        /// O id do objeto <c>Customer</c> criado pelo Stripe no registro de uma conta.
        /// O objeto <c>Customer</c> do Stripe é utilizado para vincular pagamentos já existentes
        /// a um usuário.
        /// Para ler mais, acesse a <a
        /// href="https://stripe.com/docs/api/checkout/sessions/create#create_checkout_session-payment_intent_data-setup_future_usage">documentação oficial</a>.
        /// </summary>
        public string? StripeCustomerId { get; set; } = null;

        /// <summary>
        /// Identifica um usuário que se cadastrou no pré-lançamento e possui descontos vitalícios.
        /// </summary>
        public bool IsPremium { get; set; } = false;

        // TODO change to a table where stores accounts without sync done yet.
        // then, when the sync is done, deletes the record.

        /// <summary>
        /// Define se a sincronização com os dados da B3 já foi realizada no cadastro do usuário.
        /// </summary>
        public bool IsB3SyncDone { get; set; } = false;

        // TODO change to a table where stores accounts that didnt validate the code yet.
        // then, when the accounts validates it, deletes the record.

        /// <summary>
        /// Define se o código de validação de cadastro já foi confirmado pelo usuário.
        /// </summary>
        public bool AuthenticationCodeValidated { get; set; } = false;

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
        #endregion

        #region Plans
        public Plan Plan { get; protected set; }

        /// <summary>
        /// Inicialmente, todos os investidores iniciarão com o plano gratuito.
        /// </summary>
        public int PlanId { get; set; } = PlansConstants.Free;


        /// <summary>
        /// Define se um plano está expirado.
        /// Esse evento é definido quando o webhook do Stripe é consumido no evento <c>customer.subscription.deleted</c>.
        /// </summary>
        public bool IsPlanExpired { get; set; } = false;

        #endregion

        public void HashPassword(string password)
        {
            // Move to object property set
            Password = BCryptHelper.HashPassword(password, BCryptHelper.GenerateSalt());
        }
    }

    public partial class AccountValidator : AbstractValidator<Account>
    {
        private readonly Regex HasNumber = new("[0-9]+");
        private readonly Regex HasUpperChar = new("[A-Z]+");
        private readonly Regex HasLowerChar = new("[a-z]+");
        private readonly Regex HasMinMaxChars = new(".{8,60}");
        private readonly Regex IsValidEmail = new(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        private readonly Regex IsValidCPF = new(@"(^\d{3}\.\d{3}\.\d{3}\-\d{2}$)");

        private const int NameMinLength = 3;
        private const int NameMaxLength = 20;

        public AccountValidator()
        {
            RuleFor(c => c.Name)
                .MinimumLength(NameMinLength)
                .WithMessage($"Nome de usuário deve conter no mínimo {NameMinLength} caracteres.");

            RuleFor(c => c.Name)
                .MaximumLength(NameMaxLength)
                .WithMessage($"Nome de usuário deve conter no máximo {NameMaxLength} caracteres.");

            RuleFor(c => c.Email)
                .Must(c => IsValidEmail.IsMatch(c.ToString()))
                .WithMessage($"O endereço de e-mail informado não é válido.");

            RuleFor(c => c.CPF)
                .Must(c => IsValidCPF.IsMatch(c.ToString()))
                .WithMessage($"O CPF informado não é válido.");

            RuleFor(c => c.Password)
                .Must(c => HasNumber.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo um número.");
            RuleFor(c => c.Password)
                .Must(c => HasUpperChar.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo uma letra maiúscula.");
            RuleFor(c => c.Password)
                .Must(c => HasLowerChar.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo uma letra minúscula.");
            RuleFor(c => c.Password)
                .Must(c => HasMinMaxChars.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo 8 dígitos.");
        }
    }
}
