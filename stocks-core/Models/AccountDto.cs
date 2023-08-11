using stocks_infrastructure.Models;

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

        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Email { get; init; }
        public string Password { get; init; }
        public string CPF { get; init; }
        public Plan Plan { get; init; }
    }
}
