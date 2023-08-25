namespace stocks_core.Services.Hangfire.EmailCodeRemover
{
    public interface IEmailCodeRemoverHangfire
    {
        /// <summary>
        /// Remove registros da tabela EmailCode caso um registro tenha sido inserido há mais de 10 minutos atrás.
        /// </summary>
        void Execute();
    }
}
