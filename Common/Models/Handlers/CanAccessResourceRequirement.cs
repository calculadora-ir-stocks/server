using Microsoft.AspNetCore.Authorization;

namespace Common.Models.Handlers
{
    public class CanAccessResourceRequirement : BaseRequirement, IAuthorizationRequirement
    {
        public CanAccessResourceRequirement(string scope) : base(scope)
        {
        }
    }
}
