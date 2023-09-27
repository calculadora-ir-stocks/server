using Billing.Dtos;

namespace Infrastructure.Repositories.Plan
{
    public interface IPlanRepository
    {
        IEnumerable<PlanDto> GetAll();
        Models.Plan GetByAccountId(Guid accountId);
        void Update(Models.Plan plan);
    }
}
