﻿using Infrastructure.Dtos;

namespace Infrastructure.Repositories.AverageTradedPrice
{
    public interface IAverageTradedPriceRepostory
    {
        Task AddAsync(Models.AverageTradedPrice averageTradedPrices);
        Task<IEnumerable<AverageTradedPriceDto>> GetAverageTradedPrices(Guid accountId, IEnumerable<string>? tickers = null);
        Task UpdateAllAsync(IEnumerable<AverageTradedPriceDto> averageTradedPrices);
        Task AddAllAsync(List<Models.AverageTradedPrice> averageTradedPrices);
        Task RemoveAllAsync(IEnumerable<string?> tickers, Guid id);
    }
}
