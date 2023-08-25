namespace stocks_infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class EmailCode : BaseEntity
    {
        public EmailCode(string code, Guid accountId, Account account)
        {
            Code = code;
            AccountId = accountId;
            Account = account;
        }

        public EmailCode()
        {
        }

        public string Code { get; set; }
        public Guid AccountId { get; set; }
        public Account Account { get; set; }
    }
}
