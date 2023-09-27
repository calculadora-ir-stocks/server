using Billing.Dtos;

namespace Core.Services.Plan
{
    public interface IPlanService
    {
        IEnumerable<PlanDto> GetAll();
        Infrastructure.Models.Plan GetByAccountId(Guid accountId);
    }
}
