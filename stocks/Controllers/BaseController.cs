using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace stocks.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
    }
}
