using Billing.Dtos;

namespace Infrastructure.Repositories.Plan
{
    public interface IPlanRepository
    {
        IEnumerable<StripePlanDto> GetAllStripePlans();
        IEnumerable<Models.Plan> GetAllAccountPlans();
        Models.Plan GetByAccountId(Guid accountId);
        void Update(Models.Plan plan);
    }
}
