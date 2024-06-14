namespace Infrastructure.Repositories.BonusShare
{
    public interface IBonusShareRepository
    {
        Task<Models.BonusShare?> GetByTickerAndDate(string ticker, DateTime date);
    }
}