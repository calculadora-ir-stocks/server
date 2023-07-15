using Hangfire;
using stocks_scheduled_job.Services;

namespace stocks_scheduled_job.Hangfire
{
    public class HangfireJobScheduler
    {
        private const string AverageTradedPriceUpdaterJobName = nameof(AveragePriceUpdaterService);

        public static void ScheduleJob()
        {
            RecurringJob.RemoveIfExists(AverageTradedPriceUpdaterJobName);

            RecurringJob.AddOrUpdate<AveragePriceUpdaterService>(
                AverageTradedPriceUpdaterJobName,
                x => Console.WriteLine("Hello, World"),
                Cron.Minutely
            );

            RecurringJob.TriggerJob(AverageTradedPriceUpdaterJobName);
        }
    }
}
