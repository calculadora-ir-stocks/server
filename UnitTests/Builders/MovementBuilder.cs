using Core.Constants;
using Core.Models.B3;

namespace stocks_unit_tests.Builders
{
    public class MovementBuilder
    {
        private Movement.Root root = new();
        private static readonly Random random = new();

        public MovementBuilder()
        {
            root.Data = new();
            root.Data.EquitiesPeriods = new();
            root.Data.EquitiesPeriods.EquitiesMovements = new();
        }

        public Movement.Root Build()
        {
            return root;
        }

        public MovementBuilder Create(string movementType, double operationValue, int quantity, string? assetType = null, string? ticker = null)
        {
            Movement.EquitMovement movement = new()
            {
                MovementType = movementType,
                OperationValue = operationValue,
                EquitiesQuantity = quantity,
                TickerSymbol = ticker ?? GetRandomTickerSymbol(),
                AssetType = assetType ?? GetRandomAssetType(),
            };

            root.Data.EquitiesPeriods.EquitiesMovements.Add(movement);

            return this;
        }

        private static string GetRandomTickerSymbol()
        {
            string[] tickers = new string[8]
            {
                "PETR4",
                "GOOGL34",
                "AMZO34",
                "IVVB11",
                "VALE3",
                "ITUB4",
                "BBDC4",
                "BBAS3"
            };

            return tickers[random.Next(0, tickers.Length)];
        }

        private static string GetRandomAssetType()
        {
            string[] assetTypes = new string[6]
            {
                B3ResponseConstants.Stocks,
                B3ResponseConstants.ETFs,
                B3ResponseConstants.Gold,
                B3ResponseConstants.FIIs,
                B3ResponseConstants.InvestmentsFunds,
                B3ResponseConstants.BDRs,
            };

            return assetTypes[random.Next(0, assetTypes.Length)];
        }
    }
}
