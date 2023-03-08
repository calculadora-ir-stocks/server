using stocks.DTOs.Auth;

namespace stocks.Services.Auth
{
    public interface IAuthService
    {
        void SignUp(SignUpRequest request);
        string? SignIn(SignInRequest request);
    }
}
