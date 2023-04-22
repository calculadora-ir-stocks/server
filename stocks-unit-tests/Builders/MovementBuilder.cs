using stocks_core.Constants;
using stocks_core.DTOs.B3;

namespace stocks_unit_tests.Builders
{
    public class MovementBuilder
    {
        private Movement.Root _root = new();
        private static readonly Random _random = new();

        public MovementBuilder()
        {
            _root.Data = new();
            _root.Data.EquitiesPeriods = new();
            _root.Data.EquitiesPeriods.EquitiesMovements = new();
        }

        public Movement.Root Build()
        {
            return _root;
        }

        public MovementBuilder WithBuy(double price, int quantity, string ticker)
        {
            Movement.EquitMovement movement = new()
            {
                AssetType = GetRandomAssetType(),
                MovementType = "Compra",
                TickerSymbol = ticker,
                OperationValue = price,
                EquitiesQuantity = quantity
            };

            _root.Data.EquitiesPeriods.EquitiesMovements.Add(movement);

            return this;
        }

        public MovementBuilder WithSell(double price, int quantity, string ticker)
        {
            Movement.EquitMovement movement = new()
            {
                AssetType = GetRandomAssetType(),
                MovementType = "Venda",
                TickerSymbol = ticker,
                OperationValue = price,
                EquitiesQuantity = quantity
            };

            _root.Data.EquitiesPeriods.EquitiesMovements.Add(movement);

            return this;
        }

        private static string GetRandomTickerSymbol()
        {
            string[] tickers = new string[4]
            {
                "PETR4",
                "GOOGL34",
                "AMZO34",
                "IVVB11"
            };

            return tickers[_random.Next(0, tickers.Length)];
        }

        private static string GetRandomAssetType()
        {
            string[] assetTypes = new string[6]
            {
                AssetMovementTypes.Stocks,
                AssetMovementTypes.ETFs,
                AssetMovementTypes.Gold,
                AssetMovementTypes.FIIs,
                AssetMovementTypes.FundInvestments,
                AssetMovementTypes.BDRs,
            };

            return assetTypes[_random.Next(0, assetTypes.Length)];
        }
    }
}
