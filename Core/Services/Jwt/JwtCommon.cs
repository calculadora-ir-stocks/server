using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Common;
using Common.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Services.Jwt
{
    public class JwtCommon : IJwtCommon
    {
        private readonly JwtProperties appSettings;

        /// <summary>
        /// Claim que determina se o plano de um usuário está expirado.
        /// </summary>
        private const string IsPlanExpired = "pln";

        public JwtCommon(IOptions<JwtProperties> appSettings)
        {
            this.appSettings = appSettings.Value;
        }

        public string GenerateToken(JwtDetails account)
        {
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            var signinCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            /** 
             * TODO utilizar sistema de refresh token e verificar a validade do plano de um usuário
             * através do claim pln ao invés de validar em cada request do controller TaxesController.
             */

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(IsPlanExpired, account.IsPlanExpired.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: appSettings.Issuer,
                audience: appSettings.Audience,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: signinCredentials,
                claims: claims
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Guid? CreateToken(string? token)
        {
            if (token == null)
                return null;

            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
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
