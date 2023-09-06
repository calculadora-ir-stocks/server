using Api.DTOs.Auth;

namespace Api.Services.Auth
{
    public interface IAuthService
    {
        Task SignUp(SignUpRequest request);
        string? SignIn(SignInRequest request);
    }
}
