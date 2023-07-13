using stocks_common.Enums;

namespace stocks_common.Models
{
    public class AverageTradedPriceDetails
    {
        public AverageTradedPriceDetails(string tickerSymbol, double averageTradedPrice, double totalBought, int tradedQuantity, Asset assetTypeId)
        {
            TickerSymbol = tickerSymbol;
            AverageTradedPrice = averageTradedPrice;
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
            AssetType = assetTypeId;
        }

        public string TickerSymbol { get; init; }
        public double AverageTradedPrice { get; protected set; }
        public double TotalBought { get; protected set; }
        public int TradedQuantity { get; protected set; }
        public Asset AssetType { get; init; }

        /// <summary>
        /// Quando um ativo é vendido e o investidor não o possui mais, o preço médio desse ativo não deve
        /// mais ser levado em consideração.
        /// </summary>
        public bool SoldOut { get; protected set; } = false;

        public void UpdateValues(double totalBought, int tradedQuantity)
        {
            TotalBought = totalBought;
            TradedQuantity = tradedQuantity;
            AverageTradedPrice = totalBought / tradedQuantity;

            if (TradedQuantity == 0) SoldOut = true;
        }

        public void UpdateQuantity(int quantity)
        {
            TradedQuantity = quantity;
        }
    }
}
