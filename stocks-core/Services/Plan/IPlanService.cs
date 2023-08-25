using stocks_core.Models.Requests.Plan;

namespace stocks_core.Services.Plan
{
    public interface IPlanService
    {
        IEnumerable<stocks_infrastructure.Models.Plan> GetAll();
        void Subscribe(PlanSubscribeRequest request, CancellationToken cancellationToken);
    }
}
