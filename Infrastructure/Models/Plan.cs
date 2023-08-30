namespace Infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Plan
    {
        public Plan(int id, string name, string description, string stripeProductId)
        {
            Id = id;
            Name = name;
            Description = description;
            StripeProductId = stripeProductId;
        }

        public Plan()
        {
        }

        public int Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public string StripeProductId { get; init; }
    }
}
