using Common.Models;

namespace Api.Services.Jwt
{
    public interface IJwtCommon
    {
        public string GenerateToken(JwtDetails account);
        public Guid? CreateToken(string token);
    }
}
