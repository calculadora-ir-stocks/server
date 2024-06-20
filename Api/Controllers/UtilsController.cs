using Api.Clients.B3;
using Infrastructure.Repositories.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;

namespace Api.Controllers
{
    [AllowAnonymous]
    public class UtilsController : BaseController
    {
        private readonly IB3Client b3Client;
        private readonly IAccountRepository repository;

        public UtilsController(IB3Client b3Client, IAccountRepository repository)
        {
            this.b3Client = b3Client;
            this.repository = repository;
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

        [HttpDelete("hiroshima")]
        public async Task<IActionResult> Hiroshima()
        {
            var accounts = await repository.GetAll();
            foreach (var account in accounts) repository.Delete(account);

            return Ok("Bombed.");
        }

    }
}
