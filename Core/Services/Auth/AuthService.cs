using Api.DTOs.Auth;
using Api.Exceptions;
using Api.Notification;
using Api.Services.Jwt;
using Common.Enums;
using Common.Helpers;
using Common.Models;
using Core.Services.Account;
using Core.Services.Email;
using DevOne.Security.Cryptography.BCrypt;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;

namespace Api.Services.Auth
{
    public class AuthService : IAuthService
    {

        private readonly IAccountRepository accountRepository;
        private readonly IGenericRepository<Infrastructure.Models.Account> accountGenericRepository;
        private readonly IAccountService accountService;

        private readonly IJwtCommon jwtUtils;

        private readonly NotificationContext notificationContext;

        private readonly ILogger<AuthService> logger;

        public AuthService(
            IAccountRepository accountRepository,
            IGenericRepository<Infrastructure.Models.Account> accountGenericRepository,
            IAccountService accountService,
            IJwtCommon jwtUtils,
            NotificationContext notificationContext,
            ILogger<AuthService> logger
        )
        {
            this.accountRepository = accountRepository;
            this.accountGenericRepository = accountGenericRepository;
            this.accountService = accountService;
            this.jwtUtils = jwtUtils;
            this.notificationContext = notificationContext;
            this.logger = logger;
        }

        public string? SignIn(SignInRequest request)
        {
            try
            {
                Infrastructure.Models.Account? account = accountRepository.GetByEmail(request.Email);

                if (account is null)
                    return null;

                if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.EmailNotConfirmed))
                    throw new BadRequestException("Você ainda não confirmou o seu e-mail no cadastro da sua conta.");

                if (BCryptHelper.CheckPassword(request.Password, account.Password))
                {
                    return jwtUtils.GenerateToken(new JwtDetails
                    (
                        account.Id,
                        account.Status
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

        public async Task SignUp(SignUpRequest request)
        {
            Infrastructure.Models.Account account = new(
                request.Name,
                request.Email,
                request.Password,
                request.CPF,
                request.BirthDate,
                request.PhoneNumber
            );

            if (!IsValidSignUp(account)) return;

            try
            {
                account.HashPassword(account.Password);
                accountGenericRepository.Add(account);

                await accountService.SendEmailVerification(account.Id, account);
            } catch(Exception e)
            {
                logger.LogError($"Ocorreu um erro ao tentar registrar o usuário {account.Id}. {e.Message}");
            }
        }

        private bool IsValidSignUp(Infrastructure.Models.Account account)
        {
            try
            {
                if (accountRepository.EmailExists(account.Email))
                    throw new BadRequestException($"Um usuário com esse e-mail já está cadastrado na plataforma.");

                if (accountRepository.CPFExists(account.CPF))
                    throw new BadRequestException($"Um usuário com esse CPF já está cadastrado na plataforma.");

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
