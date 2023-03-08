using stocks.Models;

namespace stocks_core.Services.AverageTradedPrice
{
    public interface IAverageTradedPriceService
    {
        /// <summary>
        /// Calcula o preço médio de todos os ativos do investidor, levando em consideração todas as movimentações
        /// de 01-11-2019 até D-1.
        /// 
        /// Deve ser rodado apenas na primeira vez que o usuário acessa a plataforma.
        /// </summary>
        Task Insert(Guid accountId);
    }
}
