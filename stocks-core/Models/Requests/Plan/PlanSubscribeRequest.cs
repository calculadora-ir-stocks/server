using stocks_core.Models.Stripe;

namespace stocks_core.Models.Requests.Plan
{
    public class PlanSubscribeRequest
    {
        public int PlanId { get; init; }
        public Guid AccountId { get; init; }
        public CreditCardStripe CreditCardInformation { get; init; }
    }
}
