using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Common;
using Common.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Services.JwtCommon
{
    public class JwtCommonService : IJwtCommonService
    {
        private readonly JwtProperties properties;

        /// <summary>
        /// Claim que determina o status de um usuário.
        /// </summary>
        private const string AccountStatus = "sts";

        public JwtCommonService(IOptions<JwtProperties> properties)
        {
            this.properties = properties.Value;
        }

        public string GenerateToken(JwtContent jwtContent)
        {
            var key = Encoding.ASCII.GetBytes(properties.Secret);
            var signinCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            /** 
             * TODO utilizar sistema de refresh token e verificar a validade do plano de um usuário
             * através do claim pln ao invés de validar em cada request do controller TaxesController.
             */

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, jwtContent.Id.ToString()),
                new Claim(AccountStatus, jwtContent.Status.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: properties.Issuer,
                audience: properties.Audience,
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

            var key = Encoding.ASCII.GetBytes(properties.Secret);
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
