using Common.Models.Handlers;
using Infrastructure.Repositories.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;

namespace Api.Handler
{
    public class CanAccessResourceHandler : AuthorizationHandler<CanAccessResourceRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAccountRepository accountRepository;

        public CanAccessResourceHandler(IHttpContextAccessor httpContextAccessor, IAccountRepository accountRepository)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.accountRepository = accountRepository;
        }

        // TODO O handler de autorização está chamando o banco a cada verificação. É necessário pedir para o front-end enviar o id de usuário no
        // JWT e, com ele, comparar com o id da rota sendo requisitada.
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessResourceRequirement requirement)
        {
            var httpContext = httpContextAccessor.HttpContext!;
            string route = httpContext.Request.Path.ToString();

            // Se um Guid está sendo usado na requisição, um determinado usuário está sendo manipulado. Ele tem permissão pra manipular esse usuário?
            if (HasAGuid(route))
            {
                string jwt = httpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer", string.Empty).Replace(" ", "");
                if (jwt.IsNullOrEmpty()) return;

                JwtSecurityToken decoded = new(jwt);

                Guid accountId = await accountRepository.GetByAuth0IdAsNoTracking(decoded.Subject);

                if (route.Contains(accountId.ToString())) 
                    context.Succeed(requirement);
            } 
            else
            {
                context.Succeed(requirement);
            }
        }

        private static bool HasAGuid(string value)
        {
            return Regex.IsMatch(value, @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}");
        }
    }
}
