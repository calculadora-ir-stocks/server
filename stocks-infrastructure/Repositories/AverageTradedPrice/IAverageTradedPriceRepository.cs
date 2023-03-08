namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public interface IAverageTradedPriceRepository
    {
        bool AccountAlreadyHasAverageTradedPrice(Guid accountId);
        void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices);
    }
}
