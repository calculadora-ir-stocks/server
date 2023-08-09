namespace stocks_core.Services.Hangfire.EmailCodeRemover
{
    public interface IEmailCodeRemoverService
    {
        /// <summary>
        /// Remove registros da tabela EmailCode caso um registro tenha sido inserido a mais de 10 minutos atrás.
        /// </summary>
        void Execute();
    }
}
