using Infrastructure.Repositories.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [AllowAnonymous]
    public class UtilsController : BaseController
    {
        private readonly IAccountRepository accountRepository;

        public UtilsController(IAccountRepository accountRepository)
        {
            this.accountRepository = accountRepository;
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllAccounts()
        {
            return Ok(await accountRepository.GetAll());
        }
    }
}
