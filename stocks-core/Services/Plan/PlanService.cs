using stocks.Repositories;

namespace stocks_core.Services.Plan
{
    public class PlanService : IPlanService
    {
        private readonly IGenericRepository<stocks_infrastructure.Models.Plan> genericRepositoryPlan;

        public PlanService(IGenericRepository<stocks_infrastructure.Models.Plan> genericRepositoryPlan)
        {
            this.genericRepositoryPlan = genericRepositoryPlan;
        }

        public IEnumerable<stocks_infrastructure.Models.Plan> GetAll()
        {
            return genericRepositoryPlan.GetAll();
        }
    }
}
