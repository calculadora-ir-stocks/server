namespace stocks.DTOs.Auth
{
    public class SignUpRequest
    {
        public SignUpRequest(string name, string email, string cpf, string password)
        {
            Name = name;
            Email = email;
            CPF = cpf;
            Password = password;
        }

        public string Name { get; protected set; }
        public string Email { get; protected set; }
        public string CPF { get; protected set; }
        public string Password { get; protected set; }
    }
}
