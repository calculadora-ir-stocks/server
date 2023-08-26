namespace Core.Models.Responses
{
    public class B3ResponseDetails
    {
        public B3ResponseDetails(List<AssetIncomeTaxes> assets, List<AverageTradedPriceDetails> averageTradedPrices)
        {
            Assets = assets;
            AverageTradedPrices = averageTradedPrices;
        }

        public List<AssetIncomeTaxes> Assets { get; init; }
        public List<AverageTradedPriceDetails> AverageTradedPrices { get; init; }
    }
}
