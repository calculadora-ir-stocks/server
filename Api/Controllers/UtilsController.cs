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

        /// <summary>
        /// Retorna todos os códigos de validação enviados por e-mail. Útil para não precisar enviar pro
        /// seu próprio e-mail.
        /// </summary>
        /// <returns></returns>
        [HttpGet("validation-codes")]
        public IActionResult GetAllValidationCodes()
        {
            return Ok(emailCodeRepository.GetAll());
        }

        /// <summary>
        /// Deleta todos os usuários da base.
        /// </summary>
        /// <returns></returns>
        [HttpDelete("nuke")]
        public IActionResult Nuke()
        {
            accountRepository.DeleteAll();
            return Ok();
        }
    }
}
