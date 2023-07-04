namespace stocks_core.Responses
{
    public class CurrentMonthTaxesResponse
    {
        public CurrentMonthTaxesResponse(double taxes, List<Asset> tradedAssets)
        {
            Taxes = taxes;
            TradedAssets = tradedAssets;
        }

        /// <summary>
        /// O total de imposto a ser pago.
        /// </summary>
        public double Taxes { get; init; }
        public List<Asset> TradedAssets { get; init; }
    }

    public class Asset
    {
        public Asset(stocks_common.Enums.Asset assetTypeId, double taxes, double totalSold, double swingTradeProfit, double dayTradeProfit, string tradedAssets)
        {
            AssetTypeId = assetTypeId;
            Taxes = taxes;
            TotalSold = totalSold;
            SwingTradeProfit = swingTradeProfit;
            DayTradeProfit = dayTradeProfit;
            TradedAssets = tradedAssets;
        }


        /// <summary>
        /// O id do tipo de ativo sendo negociado.
        /// </summary>
        public stocks_common.Enums.Asset AssetTypeId { get; init; }

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
        public string TradedAssets { get; init; } = string.Empty;
    }

}
