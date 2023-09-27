using Billing.Dtos;

namespace Core.Services.Plan
{
    public interface IPlanService
    {
        IEnumerable<StripePlanDto> GetAll();
        Infrastructure.Models.Plan GetByAccountId(Guid accountId);
    }
}
