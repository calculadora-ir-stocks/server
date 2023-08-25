using stocks_common.Models;

namespace stocks.Services.Jwt
{
    public interface IJwtCommon
    {
        public string GenerateToken(AccountDto account);
        public Guid? CreateToken(string token);
    }
}
