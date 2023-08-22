using DevOne.Security.Cryptography.BCrypt;
using FluentValidation;
using stocks_common.Constants;
using System.Text.RegularExpressions;

namespace stocks_infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Account : BaseEntity
    {
        public Account(string name, string email, string password, string cpf)
        {
            Name = name;
            Email = email;
            Password = password;
            CPF = cpf;

            Validate(this, new AccountValidator());
        }

        private Account() { }

        #region Registration information
        public string Name { get; protected set; }
        public string Email { get; protected set; }
        public string Password { get; set; }
        public string CPF { get; protected set; }
        public bool IsPremium { get; set; } = false;
        #endregion

        #region Relationships
        /// <summary>
        /// Relação dos preços médios dos ativos negociados do investidor.
        /// </summary>
        public ICollection<AverageTradedPrice>? AverageTradedPrices { get; protected set; }

        /// <summary>
        /// Relação dos impostos devidos pelo investidor.
        /// </summary>
        public ICollection<IncomeTaxes>? IncomeTaxes { get; set; }

        /// <summary>
        /// Código enviado para o e-mail para confirmar veracidade da conta.
        /// Pode ser um código de autenticação ou um código para alteração de senha.
        /// </summary>
        public EmailCode EmailCode { get; set; }
        #endregion

        public bool AuthenticationCodeValidated { get; set; } = false;

        #region Plans
        public Plan Plan { get; protected set; }

        /// <summary>
        /// Inicialmente, todos os investidores iniciarão com o plano gratuito.
        /// </summary>
        public int PlanId { get; protected set; } = PlansConstants.Free;

        private DateTime planStartDate = DateTime.UtcNow;

        /// <summary>
        /// Data de início do plano.
        /// É atualizado toda vez que um usuário assina um plano.
        /// </summary>
        public DateTime PlanStartDate
        {
            set
            {
                planStartDate = value;
                IsPlanExpired = false;
            }
            get { return planStartDate; }
        }

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
        private readonly Regex hasNumber = new("[0-9]+");
        private readonly Regex hasUpperChar = new("[A-Z]+");
        private readonly Regex hasLowerChar = new("[a-z]+");
        private readonly Regex hasMinMaxChars = new(".{8,60}");
        private readonly Regex isValidEmail = new(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        private readonly Regex isValidCPF = new(@"(^\d{3}\.\d{3}\.\d{3}\-\d{2}$)");

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

            RuleFor(c => c.CPF)
                .Must(c => isValidEmail.IsMatch(c.ToString()))
                .WithMessage($"O endereço de e-mail informado não é válido.");

            RuleFor(c => c.CPF)
                .Must(c => isValidCPF.IsMatch(c.ToString()))
                .WithMessage($"O CPF informado não é válido.");

            RuleFor(c => c.Password)
                .Must(c => hasNumber.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo um número.");
            RuleFor(c => c.Password)
                .Must(c => hasUpperChar.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo uma letra maiúscula.");
            RuleFor(c => c.Password)
                .Must(c => hasLowerChar.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo uma letra minúscula.");
            RuleFor(c => c.Password)
                .Must(c => hasMinMaxChars.IsMatch(c.ToString()))
                .WithMessage($"A sua senha deve conter no mínimo 8 dígitos.");
        }
    }
}
