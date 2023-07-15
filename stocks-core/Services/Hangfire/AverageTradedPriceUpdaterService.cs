namespace stocks_core.Services.Hangfire
{
    public class AverageTradedPriceUpdaterService : IAverageTradedPriceUpdaterService
    {
        public void Execute()
        {
            Console.WriteLine("Updating average prices.");
        }
    }
}
