namespace Infrastructure.Models
{
    public class Plan : BaseEntity
    {
        public Plan(string name, Guid accountId, Account account, DateTime expiresAt)
        {
            Name = name;
            AccountId = accountId;
            Account = account;
            ExpiresAt = expiresAt;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Plan()
        {
        }

        public string Name { get; set; }
        public Guid AccountId { get; init; }
        public Account Account { get; init; }
        public DateTime ExpiresAt { get; set; }
    }
}
