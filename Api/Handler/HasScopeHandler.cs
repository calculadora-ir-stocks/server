using Common.Models;
using Microsoft.AspNetCore.Authorization;

namespace Api.Handler
{
    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            // If user doesn't have the scope claim and the request is not coming from an trusted issuer,
            // the handler ends the process.
            if (!context.User.HasClaim(c => c.Type == "scope" && c.Issuer == requirement.Issuer))
                return Task.CompletedTask;

            string[] scopes = context.User.FindFirst(c => c.Type == "scope" && c.Issuer == requirement.Issuer)!.Value.Split(' ');

            if (scopes.Any(s => s == requirement.Scope))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
