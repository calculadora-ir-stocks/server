namespace Core.Services.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// Envia um código de verificação para a conta especificada.
        /// </summary>
        Task SendEmail(Infrastructure.Models.Account account, string code, string subject, string htmlContent);

        /// <summary>
        /// O usuário poderá enviar códigos de verificação a cada 10 minutos.
        /// </summary>
        bool CanSendEmailForUser(Guid accountId);

        /// <summary>
        /// Valida o código de verificação fornecida pelo usuário especificado.
        /// Se for válido, altera a propriedade <c>AuthenticationCodeValidated</c> para verdadeiro
        /// e, em seguida, cria um Stripe Customer.
        /// </summary>
        bool IsVerificationEmailValid(Guid accountId, string code);
    }
}
