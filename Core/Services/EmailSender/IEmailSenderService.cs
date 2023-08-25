namespace stocks_core.Services.EmailSender
{
    public interface IEmailSenderService
    {
        /// <summary>
        /// Envia um código de verificação para a conta especificada.
        /// </summary>
        Task SendEmail(stocks_infrastructure.Models.Account account, string code, string subject, string htmlContent);

        /// <summary>
        /// O usuário poderá enviar códigos de verificação a cada 10 minutos.
        /// </summary>
        bool CanSendEmailForUser(Guid accountId);

        /// <summary>
        /// Valida o código de verificação fornecida pelo usuário especificado.
        /// </summary>
        bool IsVerificationEmailValid(Guid accountId, string code);
    }
}
