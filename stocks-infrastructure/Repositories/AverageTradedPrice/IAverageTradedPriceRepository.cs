namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public interface IAverageTradedPriceRepostory
    {
        bool AccountAlreadyHasAverageTradedPrice(Guid accountId);
        void InsertAll(IEnumerable<Models.AverageTradedPrice> averageTradedPrices);
        Models.AverageTradedPrice GetAverageTradedPrice(string ticker, Guid accountId);
    }
}
