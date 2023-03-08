namespace stocks.DTOs.Auth
{
    public class SignInRequest
    {
        public SignInRequest(string email, string password)
        {
            Email = email;
            Password = password;
        }

        public string Email { get; protected set; }
        public string Password { get; protected set; }
    }
}
