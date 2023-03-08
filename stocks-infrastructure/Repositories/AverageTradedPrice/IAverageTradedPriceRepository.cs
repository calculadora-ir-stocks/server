namespace stocks_infrastructure.Repositories.AverageTradedPrice
{
    public interface IAverageTradedPriceRepository
    {
        bool AccountAlreadyHasAverageTradedPrice(Guid accountId);
    }
}
