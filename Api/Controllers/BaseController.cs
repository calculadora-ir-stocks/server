using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
    }
}
