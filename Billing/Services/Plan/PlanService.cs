using Billing.Dtos;
using Infrastructure.Repositories.Plan;

namespace Core.Services.Plan
{
    public class PlanService : IPlanService
    {
        private readonly IPlanRepository planRepository;

        public PlanService(IPlanRepository planRepository)
        {
            this.planRepository = planRepository;
        }

        public IEnumerable<PlanDto> GetAll()
        {
            return planRepository.GetAll();
        }

        public Infrastructure.Models.Plan GetByAccountId(Guid accountId)
        {
            return planRepository.GetByAccountId(accountId);
        }
    }
}
