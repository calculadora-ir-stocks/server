namespace stocks_core.Services.Hangfire
{
    public interface IAverageTradedPriceUpdaterService
    {
        /// <summary>
        /// Todo dia 01, o preço médio dos ativos operados no mês passado é atualizado na base de dados.
        /// </summary>
        void Execute();
    }
}
