using DevOne.Security.Cryptography.BCrypt;
using FluentValidation;
using stocks.Commons.Constants;
using stocks.Enums;
using stocks_infrastructure.Models;
using System.Text.RegularExpressions;

namespace stocks.Models
{
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

        private Account() {}

        public string Name { get; protected set; }
        public string Email { get; protected set; }
        public string Password { get; protected set; }
        public string CPF { get; protected set; }
        public AverageTradedPrice? AverageTradedPrice { get; protected set; }
        public Plan Plan { get; protected set; } = Plan.Default;

        public void HashPassword(string password)
        {
            Password = BCryptHelper.HashPassword(password, BCryptHelper.GenerateSalt());
        }
    }

    public partial class AccountValidator : AbstractValidator<Account>
    {
        private Regex hasNumber = new Regex("[0-9]+");
        private Regex hasUpperChar = new Regex("[A-Z]+");
        private Regex hasLowerChar = new Regex("[a-z]+");
        private Regex hasMinMaxChars = new Regex(".{8,60}");
        private Regex isValidEmail = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        private Regex isValidCPF = new Regex(@"(^\d{3}\.\d{3}\.\d{3}\-\d{2}$)");

        public AccountValidator()
        {
            RuleFor(c => c.Name)
                .MinimumLength(Validators.UsernameMinLength)
                .WithMessage($"Nome de usuário deve conter no mínimo {Validators.UsernameMinLength} caracteres.");

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
