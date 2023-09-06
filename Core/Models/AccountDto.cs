namespace Common.Models
{
    public class AccountDto
    {
        public AccountDto(Guid id, int planId)
        {
            Id = id;
            PlanId = planId;
        }

        public Guid Id { get; init; }
        public int PlanId { get; init; }
    }
}
