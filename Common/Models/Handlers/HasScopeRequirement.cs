using Microsoft.AspNetCore.Authorization;

namespace Common.Models.Handlers
{
    public class HasScopeRequirement : BaseRequirement, IAuthorizationRequirement
    {
        public HasScopeRequirement(string scope, string issuer) : base(scope)
        {
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }

        public string Issuer { get; }
    }
}
