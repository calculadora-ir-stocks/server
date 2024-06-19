using Api.Clients.B3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;

namespace Api.Controllers
{
    [AllowAnonymous]
    public class UtilsController : BaseController
    {
        private readonly IB3Client b3Client;

        public UtilsController(IB3Client b3Client)
        {
            this.b3Client = b3Client;
        }

        [HttpGet("ping")]
        public IActionResult PingPong()
        {
            return Ok("pong!");
        }

        [HttpGet("ping/b3")]
        public async Task <IActionResult> B3PingPong()
        {
            var statusCode = await b3Client.B3HealthCheck();
            return Ok(new { statusCode = statusCode });
        }

    }
}
