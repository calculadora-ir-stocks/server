using Core.Models.Requests.Plan;

namespace Core.Services.Plan
{
    public interface IPlanService
    {
        IEnumerable<Infrastructure.Models.Plan> GetAll();
        void Subscribe(PlanSubscribeRequest request, CancellationToken cancellationToken);
    }
}
