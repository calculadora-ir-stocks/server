namespace Core.Services.Email
{
    [Obsolete("E-mails eram enviados para a confirmação de cadastro. Hoje, com o Auth0, eles não são mais necessários" +
    " - porém, a integração está feita e será utilizada quando necessário.")]
    public interface IEmailService
    {
        /// <summary>
        /// Envia um e-mail para a conta especificada.
        /// </summary>
        [Obsolete("E-mails eram enviados para a confirmação de cadastro. Hoje, com o Auth0, eles não são mais necessários" +
        " - porém, a integração está feita e será utilizada quando necessário.")]
        Task Send(string subject, string htmlContent);
    }
}
