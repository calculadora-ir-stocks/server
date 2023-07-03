namespace stocks_core.Requests.IncomeTaxes
{
    public record AssetsIncomeTaxesRequest
    {
        public Guid AccountId { get; init; }
        public string Month { get; init; }
    }
}
