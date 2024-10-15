using Microsoft.AspNetCore.Authorization;

namespace Common.Models.Handlers
{
    public class HasScopeRequirement : BaseRequirement, IAuthorizationRequirement
    {
        public HasScopeRequirement(string scope, string issuer) : base(scope)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }

        public string Issuer { get; }
    }
}
