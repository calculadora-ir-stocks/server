using Api.DTOs.Auth;
using Api.Services.JwtCommon;
using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Common.Models;
using Core.Models.Api.Responses;
using Core.Notification;
using Core.Services.Account;
using DevOne.Security.Cryptography.BCrypt;
using Infrastructure.Models;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;

namespace Api.Services.Auth
{
    public class AuthService : IAuthService
    {

        private readonly IAccountRepository accountRepository;
        private readonly IGenericRepository<Account> accountGenericRepository;
        private readonly IAccountService accountService;

        private readonly IJwtCommonService jwtUtils;

        private readonly NotificationManager notificationManager;

        private readonly ILogger<AuthService> logger;

        public AuthService(
            IAccountRepository accountRepository,
            IGenericRepository<Account> accountGenericRepository,
            IAccountService accountService,
            IJwtCommonService jwtUtils,
            NotificationManager notificationManager,
            ILogger<AuthService> logger
        )
        {
            this.accountRepository = accountRepository;
            this.accountGenericRepository = accountGenericRepository;
            this.accountService = accountService;
            this.jwtUtils = jwtUtils;
            this.notificationManager = notificationManager;
            this.logger = logger;
        }

        public (string?, Guid) SignIn(SignInRequest request)
        {
            try
            {
                Account? account = accountRepository.GetByEmail(request.Email);

                if (account is null)
                    return (null, Guid.Empty);

                if (account.Status == EnumHelper.GetEnumDescription(AccountStatus.EmailNotConfirmed))
                    throw new BadRequestException("Você ainda não confirmou o seu e-mail no cadastro da sua conta.");

                if (BCryptHelper.CheckPassword(request.Password, account.Password))
                {
                    return (jwtUtils.GenerateToken(new JwtContent(account.Id, account.Status)), account.Id);
                }

                return (null, Guid.Empty);
            }
            catch (Exception e)
            {
                logger.LogError($"Uma exceção ocorreu ao tentar autenticar um usuário. Erro: {e.Message}");
                throw;
            }
        }

        public async Task<SignUpResponse> SignUp(SignUpRequest request)
        {
            Account account = new(
                request.Name,
                request.Email,
                request.Password,
                request.CPF,
                request.BirthDate,
                request.PhoneNumber
            );

            ThrowExceptionIfSignUpIsInvalid(account, request.IsTOSAccepted);

            if (account.IsInvalid)
            {
                notificationManager.AddNotifications(account.ValidationResult);
                return new SignUpResponse(Guid.Empty, string.Empty);
            }

            try
            {
                account.HashPassword(account.Password);
                accountGenericRepository.Add(account);

                await accountService.SendEmailVerification(account.Id, account);

                var jwt = jwtUtils.GenerateToken(new JwtContent(
                        account.Id,
                        account.Status
                    ));

                return new SignUpResponse(account.Id, jwt);
            } catch(Exception e)
            {
                logger.LogError($"Ocorreu um erro ao tentar registrar o usuário {account.Id}. {e.Message}");
                throw;
            }
        }

        private void ThrowExceptionIfSignUpIsInvalid(Account account, bool isTOSAccepted)
        {
            try
            {
                if (!isTOSAccepted)
                    throw new BadRequestException("Os termos de uso precisam ser aceitos.");

                if (accountRepository.EmailExists(account.Email))
                    throw new BadRequestException($"Um usuário com esse e-mail já está cadastrado na plataforma.");

                if (accountRepository.CPFExists(account.CPF))
                    throw new BadRequestException($"Um usuário com esse CPF já está cadastrado na plataforma.");
            } catch (Exception e)
            {
                logger.LogError($"Ocorreu um erro tentar validar se o usuário {account.Id} já está cadastrado" +
                    $"na plataforma. {e.Message}");
                throw;
            }
        }
    }
}
