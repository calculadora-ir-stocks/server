namespace Billing
{
    public class StripeEventId
    {
        public StripeEventId(string id)
        {
            Id = id;
        }

        public string Id { get; init; }
    }
}
