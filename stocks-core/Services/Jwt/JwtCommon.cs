using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using stocks_common;
using stocks_common.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace stocks.Services.Jwt
{
    public class JwtCommon : IJwtCommon
    {
        private readonly AppSettings _appSettings;

        public JwtCommon(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public string GenerateToken(AccountDto account)
        {
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var signinCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _appSettings.Issuer,
                audience: _appSettings.Audience,
                expires: DateTime.Now.AddMonths(12),
                signingCredentials: signinCredentials,
                claims: claims
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Guid? CreateToken(string? token)
        {
            if (token == null)
                return null;

            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var signinCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == "Id").Value;

                return Guid.Parse(userId);
            }
            catch
            {
                return null;
            }
        }
    }
}
