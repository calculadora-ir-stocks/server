namespace stocks_core.Services.Plan
{
    public interface IPlanService
    {
        IEnumerable<stocks_infrastructure.Models.Plan> GetAll();
    }
}
