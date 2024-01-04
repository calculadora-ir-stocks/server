using Common.Enums;
using Core.Calculators;
using Core.Calculators.Assets;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace stocks_unit_tests.Business
{
    public class StocksIncomeTaxesTests
    {
        private readonly IIncomeTaxesCalculator stocksCalculator = new StocksIncomeTaxes();

        #region Day-trade unit tests

        [Theory(DisplayName = "Deve aplicar 20% de imposto em operações day-trade sob ações quando houver lucro.")]
        [MemberData(nameof(ProfitDayTradeData))]
        public void TestDayTradeProfit(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)stocks.DayTradeProfit;
            double expectedTotalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();
                
            Assert.Equal((double)expectedTaxes, stocks.Taxes);
            Assert.Equal(expectedTotalSold, stocks.TotalSold);
        }

        [Theory(DisplayName = "Não deve aplicar 20% de imposto em operações day-trade sob ações quando houver prejuizo.")]
        [MemberData(nameof(LossDayTradeData))]
        public void TestDayTradeLoss(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = 0;
            double expectedTotalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();
                
            Assert.Equal((double)expectedTaxes, stocks.Taxes);
            Assert.Equal(expectedTotalSold, stocks.TotalSold);
        }

        public static IEnumerable<object[]> ProfitDayTradeData()
        {
            List<Movement.EquitMovement> movements = new();

            double buy = 10000;
            double sell = 15054.43;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 9432;
            sell = 11324;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), true));

            yield return new object[]
            {
                movements
            };
        }

        public static IEnumerable<object[]> LossDayTradeData()
        {
            List<Movement.EquitMovement> movements = new();

            int buy = 12324;
            double sell = 10324.32;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 5465;
            sell = 3487;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 2355;
            sell = 1324;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), true));

            yield return new object[]
            {
                movements
            };
        }

        #endregion

        #region Swing-trade unit tests

        [Theory(DisplayName = "Deve aplicar 15% de impostos em operações swing-trade sob ações quando > 20k forem vendidos.")]
        [MemberData(nameof(ProfitSwingTradeDataMoreThan20k))]
        public void TestSwingTradeProfitMoreThan20kSold(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocks.SwingTradeProfit;
            double expectedTotalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();

            Assert.Equal((double)expectedTaxes, stocks.Taxes);
            Assert.Equal(expectedTotalSold, stocks.TotalSold);
        }

        [Theory(DisplayName = "Não deve aplicar 15% de impostos em operações swing-trade sob ações quando < 20k forem vendidos.")]
        [MemberData(nameof(ProfitSwingTradeDataLessThan20k))]
        public void TestSwingTradeProfitLessThan20kSold(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocksResponse = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocksResponse.SwingTradeProfit;
            double expectedTotalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();

            Assert.Equal(0, stocksResponse.Taxes);
            Assert.Equal(expectedTotalSold, stocksResponse.TotalSold);
        }

        public static IEnumerable<object[]> ProfitSwingTradeDataMoreThan20k()
        {
            List<Movement.EquitMovement> movements = new()
            {
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 24043, 1, 24043, new DateTime(2023, 01, 01), false),
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 28394, 1, 28394, new DateTime(2023, 01, 01), false),
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 9032, 1, 9032, new DateTime(2023, 01, 01), false),
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 10492, 1, 10492, new DateTime(2023, 01, 01), false)
            };

            // Lucro em operações com vendas em > 20k.
            yield return new object[]
            {
                movements
            };
        }

        public static IEnumerable<object[]> ProfitSwingTradeDataLessThan20k()
        {
            List<Movement.EquitMovement> movements = new();
            int movementsQuantity = Random.Shared.Next(0, 100);

            for (int i = 0; i <= movementsQuantity; i++)
            {
                // lucro
                int buy = Random.Shared.Next(0, 10);
                int sell = Random.Shared.Next(0, 100);

                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), false));
                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), false));
            }

            // Lucro em operações com vendas em < 20k.
            yield return new object[]
            {
                movements
            };
        }

        #endregion

        #region Day-trade and swing-trade unit tests

        [Theory(DisplayName = "Deve aplicar 20% de impostos sobre operações day-trade, mas 0% em operações swing-trade caso < 20k tenha sido vendido.")]
        [MemberData(nameof(DayTradeAndSwingTradeData))]
        public void TestBothDayTradeAndSwingTrade(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal swingTradeTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocks.SwingTradeProfit;
            decimal dayTradeTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)stocks.DayTradeProfit;

            double totalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();
            double expectedTaxes = 0;

            if (stocks.SwingTradeProfit > 0 && totalSold > 20000) expectedTaxes += (double)swingTradeTaxes;
            if (stocks.DayTradeProfit > 0) expectedTaxes += (double)dayTradeTaxes;

            Assert.Equal(totalSold, stocks.TotalSold);
            Assert.Equal(expectedTaxes, stocks.Taxes);
        }

        public static IEnumerable<object[]> DayTradeAndSwingTradeData()
        {
            // Lucro em operações de swing-trade e day-trade.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                    new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 25000, 1, 25000, new DateTime(2023, 01, 02)),
                    new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 26405, 1, 26405, new DateTime(2023, 01, 03), true),
                    new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 27000, 1, 27000, new DateTime(2023, 01, 03), true)
                }
            };
        }

        #endregion
    }
}
