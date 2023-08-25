using Microsoft.AspNetCore.Mvc;
using Core.Models.Requests.Plan;
using Core.Services.Plan;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers
{
    [Authorize]
    [Tags("Plans")]
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
