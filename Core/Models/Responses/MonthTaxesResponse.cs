using Core.Models;

namespace Core.Responses
{
    public class MonthTaxesResponse
    {
        public MonthTaxesResponse(double totalTaxes, List<Asset> tradedAssets)
        {
            TotalTaxes = totalTaxes;
            TradedAssets = tradedAssets;
        }

        /// <summary>
        /// O total de imposto a ser pago.
        /// </summary>
        public double TotalTaxes { get; init; }
        public List<Asset> TradedAssets { get; init; }
    }

    public class Asset
    {
        public Asset(Common.Enums.Asset assetTypeId, string assetTypeName, double taxes, double totalSold,
            double swingTradeProfit, double dayTradeProfit, IEnumerable<OperationDetails> assets)
        {
            AssetTypeId = assetTypeId;
            AssetTypeName = assetTypeName;
            Taxes = taxes;
            TotalSold = totalSold;
            SwingTradeProfit = swingTradeProfit;
            DayTradeProfit = dayTradeProfit;
            Assets = assets;
        }


        /// <summary>
        /// O id do tipo de ativo sendo negociado.
        /// </summary>
        public Common.Enums.Asset AssetTypeId { get; init; }

        /// <summary>
        /// O nome do tipo de ativo sendo negociado.
        /// </summary>
        public string AssetTypeName { get; init; }

        /// <summary>
        /// Total a ser pago em imposto de renda referente a um ativo.
        /// </summary>
        public double Taxes { get; init; } = 0;

        /// <summary>
        /// Total vendido do ativo.
        /// </summary>
        public double TotalSold { get; init; } = 0;

        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por swing trade.
        /// </summary>
        public double SwingTradeProfit { get; init; } = 0;

        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por day trade.
        /// </summary>
        public double DayTradeProfit { get; init; } = 0;

        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public IEnumerable<OperationDetails> Assets { get; init; }
    }
}
