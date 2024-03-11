using Api.DTOs.Auth;
using Common.Exceptions;
using Core.Notification;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Api.Services.Auth
{
    public class AuthService : IAuthService
    {

        private readonly IAccountRepository accountRepository;

        private readonly CustomerService stripeCustomerService;

        private readonly NotificationManager notificationManager;
        private readonly ILogger<AuthService> logger;

        public AuthService(
            IAccountRepository accountRepository,
            CustomerService stripeCustomerService,
            NotificationManager notificationManager,
            ILogger<AuthService> logger
        )
        {
            this.accountRepository = accountRepository;
            this.stripeCustomerService = stripeCustomerService;
            this.notificationManager = notificationManager;
            this.logger = logger;
        }

        public async Task<Infrastructure.Models.Account> SignUp(SignUpRequest request)
        {
            Infrastructure.Models.Account account = new(request.Auth0Id, request.CPF, request.BirthDate);

            await ThrowExceptionIfSignUpIsInvalid(account, request.IsTOSAccepted);

            if (account.IsInvalid)
            {
                notificationManager.AddNotifications(account.ValidationResult);
                return account;
            }

            Customer? stripeAccount = await stripeCustomerService.CreateAsync(new CustomerCreateOptions());

            account.StripeCustomerId = stripeAccount.Id;
            await accountRepository.Create(account);

            return account;
        }

        private async Task ThrowExceptionIfSignUpIsInvalid(Infrastructure.Models.Account account, bool isTOSAccepted)
        {
            try
            {                
                if (!isTOSAccepted)
                    throw new BadRequestException("Os termos de uso precisam ser aceitos.");

                if (await accountRepository.CPFExists(account.CPF))
                    throw new BadRequestException("Um usuário com esse CPF já está cadastrado na plataforma.");
            }
            catch (Exception e)
            {
                logger.LogError($"Ocorreu um erro tentar validar se o usuário {account.Id} já está cadastrado " +
                    $"na plataforma. {e.Message}");
                throw;
            }
        }
    }
}
