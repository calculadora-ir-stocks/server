using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [AllowAnonymous]
    public class UtilsController : BaseController
    {
        [HttpGet("ping")]
        public IActionResult PingPong()
        {
            return Ok("pong!");
        }
    }
}
