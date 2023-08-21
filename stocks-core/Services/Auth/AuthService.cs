using DevOne.Security.Cryptography.BCrypt;
using Microsoft.Extensions.Logging;
using stocks.DTOs.Auth;
using stocks.Exceptions;
using stocks.Notification;
using stocks.Repositories;
using stocks.Repositories.Account;
using stocks.Services.Jwt;
using stocks_common.Models;
using stocks_infrastructure.Models;

namespace stocks.Services.Auth
{
    public class AuthService : IAuthService
    {

        private readonly IAccountRepository accountRepository;
        private readonly IGenericRepository<Account> accountGenericRepository;
        private readonly IJwtCommon jwtUtils;

        private readonly NotificationContext notificationContext;

        private readonly ILogger<AuthService> logger;

        public AuthService(
            IAccountRepository accountRepository,
            IGenericRepository<Account> accountGenericRepository,
            IJwtCommon jwtUtils,
            NotificationContext notificationContext,
            ILogger<AuthService> logger
        )
        {
            this.accountRepository = accountRepository;
            this.accountGenericRepository = accountGenericRepository;
            this.jwtUtils = jwtUtils;
            this.notificationContext = notificationContext;
            this.logger = logger;
        }

        public string? SignIn(SignInRequest request)
        {
            try
            {
                var account = accountRepository.GetByEmail(request.Email);

                if (account is null)
                    return null;

                if (!account.AuthenticationCodeValidated) 
                    throw new InvalidBusinessRuleException("Você ainda não confirmou o seu e-mail no cadastro da sua conta.");

                if (BCryptHelper.CheckPassword(request.Password, account?.Password))
                {
                    return jwtUtils.GenerateToken(new AccountDto
                    (
                        account!.Id,
                        account!.PlanId
                    ));
                }

                return null;
            }
            catch (Exception e)
            {
                logger.LogError($"Uma exceção ocorreu ao tentar autenticar um usuário. Erro: {e.Message}");
                throw;
            }
        }

        public void SignUp(SignUpRequest request)
        {
            Account account = new(request.Name, request.Email, request.Password, request.CPF);

            if (!IsValidSignUp(account)) return;

            account.HashPassword(account.Password);

            try
            {
                accountGenericRepository.Add(account);
            } catch(Exception e)
            {
                logger.LogError($"Ocorreu um erro ao tentar registrar o usuário {account.Id}. {e.Message}");
            }
        }

        private bool IsValidSignUp(Account account)
        {
            try
            {
                if (accountRepository.AccountExists(account.Email))
                    throw new InvalidBusinessRuleException($"Esse usuário já está cadastrado na plataforma.");

                if (account.IsInvalid)
                {
                    notificationContext.AddNotifications(account.ValidationResult);
                    return false;
                }

            } catch (Exception e)
            {
                logger.LogError($"Ocorreu um erro tentar validar se o usuário {account.Id} já está cadastrado" +
                    $"na plataforma. {e.Message}");
                throw;
            }

            return true;
        }
    }
}
