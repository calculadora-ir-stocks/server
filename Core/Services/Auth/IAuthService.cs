using Api.DTOs.Auth;

namespace Api.Services.Auth
{
    public interface IAuthService
    {
        void SignUp(SignUpRequest request);
        string? SignIn(SignInRequest request);
    }
}
