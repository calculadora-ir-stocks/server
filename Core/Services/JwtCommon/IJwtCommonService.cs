using Common.Models;

namespace Api.Services.JwtCommon
{
    public interface IJwtCommonService
    {
        public string GenerateToken(JwtContent account);
        public Guid? CreateToken(string token);
    }
}
