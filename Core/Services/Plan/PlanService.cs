using Infrastructure.Repositories;

namespace Core.Services.Plan
{
    public class PlanService : IPlanService
    {
        private readonly IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan;

        public PlanService(
            IGenericRepository<Infrastructure.Models.Plan> genericRepositoryPlan
        )
        {
            this.genericRepositoryPlan = genericRepositoryPlan;
        }

        public IEnumerable<Infrastructure.Models.Plan> GetAll()
        {
            return genericRepositoryPlan.GetAll();
        }
    }
}
