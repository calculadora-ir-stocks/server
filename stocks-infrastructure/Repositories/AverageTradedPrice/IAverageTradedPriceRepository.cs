using stocks_common.Models;
using stocks_infrastructure.Dtos;

namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public interface IAverageTradedPriceRepostory
    {
        bool AlreadyHasAverageTradedPrice(Guid accountId);
        void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices);
        Task Insert(Models.AverageTradedPrice averageTradedPrices);
        Models.AverageTradedPrice? GetAverageTradedPrice(string ticker, Guid accountId);
        Task<IEnumerable<AverageTradedPriceDto>> GetAverageTradedPrices(Guid accountId, List<string>? tickers = null);
        Task UpdateAllAsync(List<Models.AverageTradedPrice> averageTradedPrices);
        Task AddAllAsync(List<Models.AverageTradedPrice> averageTradedPrices);
        Task RemoveAllAsync(IEnumerable<string?> tickers, Guid id);
    }
}
