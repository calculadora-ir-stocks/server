using stocks.Enums;

namespace stocks_common.Models
{
    public class AccountDto
    {
        public AccountDto(Guid id, string name, string email, string password, string cpf, Plan plan)
        {
            Id = id;
            Name = name;
            Email = email;
            Password = password;
            CPF = cpf;
            Plan = plan;
        }

        public Guid Id { get; protected set; }
        public string Name { get; protected set; }
        public string Email { get; protected set; }
        public string Password { get; protected set; }
        public string CPF { get; protected set; }
        public Plan Plan { get; protected set; } = Plan.Default;
    }
}
