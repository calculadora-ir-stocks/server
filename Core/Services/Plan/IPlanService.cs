namespace Core.Services.Plan
{
    public interface IPlanService
    {
        IEnumerable<Infrastructure.Models.Plan> GetAll();
    }
}
