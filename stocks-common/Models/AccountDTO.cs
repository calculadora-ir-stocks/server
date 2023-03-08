using stocks.Enums;

namespace stocks_common.Models
{
    public class AccountDTO
    {
        public AccountDTO(Guid id, string name, string email, string password, string cPF, Plan plan)
        {
            Id = id;
            Name = name;
            Email = email;
            Password = password;
            CPF = cPF;
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
