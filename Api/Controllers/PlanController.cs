using Microsoft.AspNetCore.Mvc;
using stocks_core.Models.Requests.Plan;
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

        /// <summary>
        /// Retorna todos os planos disponíveis.
        /// </summary>
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(service.GetAll());
        }

        /// <summary>
        /// Assina um novo plano para o usuário especificado.
        /// </summary>
        [HttpPost]
        public IActionResult Subscribe([FromBody] PlanSubscribeRequest request, CancellationToken cancellationToken)
        {
            service.Subscribe(request, cancellationToken);
            return Ok();
        }
    }
}
