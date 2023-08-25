namespace Core.Services.Hangfire.AverageTradedPriceUpdater
{
    public interface IAverageTradedPriceUpdaterHangfire
    {
        /// <summary>
        /// Todo dia 01, o preço médio dos ativos operados no mês passado é atualizado na base de dados.
        /// </summary>
        Task Execute();
    }
}
