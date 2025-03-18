using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Models
{
    public class HangfireJobsHistory
    {
        public HangfireJobsHistory(HangfireJobs hangfireJobId, Guid accountId, Account account, DateTime executedAt)
        {
            HangfireJobId = hangfireJobId;
            AccountId = accountId;
            Account = account;
            ExecutedAt = executedAt;
        }

        public HangfireJobsHistory()
        {
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; init; } = Guid.NewGuid();
        public HangfireJobs HangfireJobId { get; init; }
        public Guid AccountId { get; init; }
        public Account Account { get; init; }
        public DateTime ExecutedAt { get; init; }
    }
}
