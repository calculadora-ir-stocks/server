using Infrastructure.Dtos;

namespace Infrastructure.Repositories.AverageTradedPrice
{
    public interface IAverageTradedPriceRepostory
    {
        void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices);
        Task AddAsync(Models.AverageTradedPrice averageTradedPrices);
        Models.AverageTradedPrice? GetAverageTradedPrice(string ticker, Guid accountId);
        Task<IEnumerable<AverageTradedPriceDto>> GetAverageTradedPricesDto(Guid accountId, List<string>? tickers = null);
        List<Models.AverageTradedPrice>? GetAverageTradedPrices(Guid accountId, List<string>? tickers = null);
        Task UpdateAllAsync(List<Models.AverageTradedPrice> averageTradedPrices);
        Task AddAllAsync(List<Models.AverageTradedPrice> averageTradedPrices);
        Task RemoveAllAsync(IEnumerable<string?> tickers, Guid id);
    }
}
