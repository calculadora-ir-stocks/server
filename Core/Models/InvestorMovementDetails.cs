using Infrastructure.Models;

namespace Core.Models
{
    public class InvestorMovementDetails
    {
        public InvestorMovementDetails()
        {
            Assets = new();
            AverageTradedPrices = new();
            TickerStatusAtTheEndOfTheYear = new();
        }

        /// <summary>
        /// Lista de tipos de ativos negociados (ex.: ações, BDRs, ETFs, FIIs, ouro e etc).
        /// </summary>
        public List<AssetIncomeTaxes> Assets { get; init; }
        public List<AverageTradedPriceDetails> AverageTradedPrices { get; init; }
        /// <summary>
        /// Representa o preço médio, mas do último dia operado daquele ano.
        /// </summary>
        public List<AverageTradedPriceDetails> TickerStatusAtTheEndOfTheYear { get; init; }
    }
}
