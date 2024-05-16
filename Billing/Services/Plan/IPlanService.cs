using Billing.Dtos;

namespace Core.Services.Plan
{
    public interface IPlanService
    {
        IEnumerable<StripePlanDto> GetAll();
    }
}
