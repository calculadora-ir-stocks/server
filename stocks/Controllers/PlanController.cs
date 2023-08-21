using Microsoft.AspNetCore.Mvc;
using stocks_core.Services.Plan;

namespace stocks.Controllers
{
    // TODO [Authorize]
    public class PlanController : BaseController
    {
        private readonly IPlanService service;

        public PlanController(IPlanService service)
        {
            this.service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(service.GetAll());
        }
    }
}
