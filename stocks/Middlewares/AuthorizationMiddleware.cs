using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using stocks.Models;

namespace stocks.Middlewares
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizationMiddleware : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Skips authorization
            if (IsActionAnonymous(context))
                return;

            var user = context.HttpContext.Items["User"] as Account;

            if (user is null)
                context.Result = new JsonResult(new
                { message = $"Você não pode executar essa operação. Primeiro, você deve possuir uma conta registrada." })
                { StatusCode = StatusCodes.Status401Unauthorized };
        }

        private static bool IsActionAnonymous(AuthorizationFilterContext context)
        {
            return context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        }
    }
}
