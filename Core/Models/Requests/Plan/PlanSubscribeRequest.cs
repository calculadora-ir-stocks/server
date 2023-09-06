using Core.Models.Stripe;

namespace Core.Models.Requests.Plan
{
    public class PlanSubscribeRequest
    {
        public int PlanId { get; init; }
        public Guid AccountId { get; init; }
        public CreditCardStripe CreditCardInformation { get; init; }
    }
}
