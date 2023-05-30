namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public interface IAverageTradedPriceRepostory
    {
        bool AlreadyHasAverageTradedPriceCalculated(Guid accountId);
        void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices);
        Task Insert(Models.AverageTradedPrice averageTradedPrices);
        Models.AverageTradedPrice? GetAverageTradedPrice(string ticker, Guid accountId);
        void Update(Guid id, string ticker);
        Task AddAllAsync(List<Models.AverageTradedPrice> averageTradedPrices);
    }
}
