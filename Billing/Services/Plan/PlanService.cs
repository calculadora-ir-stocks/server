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

        public IEnumerable<StripePlanDto> GetAll()
        {
            return planRepository.GetAllStripePlans();
        }
    }
}
