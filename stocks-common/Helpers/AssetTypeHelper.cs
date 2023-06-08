using stocks_common.Enums;
using stocks_core.Constants;

namespace stocks_common.Helpers
{
    public class AssetTypeHelper
    {
        public static Asset GetAssetTypeByName(string assetType)
        {
            return assetType switch
            {
                B3ResponseConstants.Stocks => Asset.Stocks,
                B3ResponseConstants.ETFs => Asset.ETFs,
                B3ResponseConstants.FIIs => Asset.FIIs,
                B3ResponseConstants.InvestmentsFunds => Asset.InvestmentsFunds,
                B3ResponseConstants.BDRs => Asset.BDRs,
                B3ResponseConstants.Gold => Asset.Gold,
                _ => throw new ArgumentNullException($"O ativo do tipo {assetType} não existe."),
            };
        }

        public static string GetNameByAssetType(Asset assetType)
        {
            return assetType switch
            {
                Asset.Stocks => B3ResponseConstants.Stocks,
                Asset.ETFs => B3ResponseConstants.ETFs,
                Asset.FIIs => B3ResponseConstants.FIIs,
                Asset.InvestmentsFunds => B3ResponseConstants.InvestmentsFunds,
                Asset.BDRs => B3ResponseConstants.BDRs,
                Asset.Gold => B3ResponseConstants.Gold,
                _ => throw new ArgumentNullException($"O ativo do tipo {assetType} não existe."),
            };
        }
    }
}
