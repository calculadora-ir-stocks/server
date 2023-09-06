using Infrastructure.Models;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [AllowAnonymous]
    public class UtilsController : BaseController
    {
        private readonly IAccountRepository accountRepository;
        private readonly IGenericRepository<EmailCode> emailCodeRepository;

        public UtilsController(IAccountRepository accountRepository, IGenericRepository<EmailCode> emailCodeRepository)
        {
            this.accountRepository = accountRepository;
            this.emailCodeRepository = emailCodeRepository;
        }

        [HttpGet("accounts")]
        public IActionResult GetAllAccounts()
        {
            return Ok(accountRepository.GetAll());
        }

        [HttpGet("validation-codes")]
        public IActionResult GetAllValidationCodes()
        {
            return Ok(emailCodeRepository.GetAll());
        }
    }
}
