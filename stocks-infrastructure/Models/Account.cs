using System.Text.RegularExpressions;
using DevOne.Security.Cryptography.BCrypt;
using FluentValidation;
using stocks.Enums;

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

        public string Name { get; protected set; }
        public string Email { get; protected set; }
        public string Password { get; set; }
        public string CPF { get; protected set; }
        public ICollection<AverageTradedPrice>? AverageTradedPrices { get; protected set; }
        public ICollection<IncomeTaxes>? IncomeTaxes { get; set; }
        public EmailCode EmailCode { get; set; }
        public bool AuthenticationCodeValidated { get; set; } = false;
        public Plan Plan { get; protected set; } = Plan.Default;

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

            RuleFor(c => c.Email)
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
