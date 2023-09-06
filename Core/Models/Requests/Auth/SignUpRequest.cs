namespace Api.DTOs.Auth
{
    public record SignUpRequest
    {
        public SignUpRequest(string name, string email, string cPF, string password, string phoneNumber)
        {
            Name = name;
            Email = email;
            CPF = cPF;
            Password = password;
            PhoneNumber = phoneNumber;
        }

        public string Name { get; protected set; }
        public string Email { get; protected set; }
        public string CPF { get; protected set; }
        public string Password { get; protected set; }
        public string PhoneNumber { get; protected set; }
    }
}
