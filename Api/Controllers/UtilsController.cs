﻿using Infrastructure.Repositories.Account;
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
        public IActionResult GetAllAccounts()
        {
            return Ok(accountRepository.GetAll());
        }

        /// <summary>
        /// Deleta todos os usuários da base.
        /// </summary>
        [HttpDelete("nuke")]
        public IActionResult Nuke()
        {
            accountRepository.DeleteAll();
            return Ok();
        }
    }
}
