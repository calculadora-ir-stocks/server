using DevOne.Security.Cryptography.BCrypt;
using Microsoft.Extensions.Logging;
using stocks.Commons.Jwt;
using stocks.DTOs.Auth;
using stocks.Exceptions;
using stocks.Models;
using stocks.Notification;
using stocks.Repositories;
using stocks.Repositories.Account;

namespace stocks.Services.Auth
{
    public class AuthService : IAuthService
    {

        private readonly IAccountRepository _accountRepository;
        private readonly IGenericRepository<Account> _accountGenericRepository;
        private readonly IJwtCommon _jwtUtils;

        private readonly NotificationContext _notificationContext;

        private readonly ILogger<AuthService> _logger;

        public AuthService(IAccountRepository accountRepository, IGenericRepository<Account> accountGenericRepository,
            IJwtCommon jwtUtils, NotificationContext notificationContext, ILogger<AuthService> logger)
        {
            _accountRepository = accountRepository;
            _accountGenericRepository = accountGenericRepository;
            _jwtUtils = jwtUtils;
            _notificationContext = notificationContext;
            _logger = logger;
        }

        public string? SignIn(SignInRequest request)
        {
            try
            {
                var account = _accountRepository.GetByEmail(request.Email);

                if (account is null)
                    return null;

                if (BCryptHelper.CheckPassword(request.Password, account?.Password))
                {
                    return _jwtUtils.GenerateToken(new stocks_common.Models.AccountDTO
                    (
                        account!.Id,
                        account!.Name,
                        account!.Email,
                        account!.Password,
                        account!.CPF,
                        account!.Plan
                    ));
                }

                return null;
            }
            catch (Exception e)
            {
                _logger.LogError($"Uma exceção ocorreu ao autenticar um usuário. Erro: {e.Message}");
                throw;
            }
        }

        public void SignUp(SignUpRequest request)
        {
            Account account = new(request.Name, request.Email, request.Password, request.CPF);

            if (IsValidSignUp(account))
            {
                account.HashPassword(account.Password);
                _accountGenericRepository.Add(account);
            }
        }

        private bool IsValidSignUp(Account account)
        {
            if (_accountRepository.AccountExists(account.Email))
                throw new InvalidBusinessRuleException($"Esse usuário já está cadastrado na plataforma.");

            if (account.IsInvalid)
            {
                _notificationContext.AddNotifications(account.ValidationResult);
                return false;
            }

            return true;
        }
    }
}
