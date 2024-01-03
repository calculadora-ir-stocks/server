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
        private readonly IIncomeTaxesCalculator stocksCalculator;

        public StocksIncomeTaxesTests()
        {
            stocksCalculator = new StocksIncomeTaxes();
        }

        #region Day-trade unit tests

        [Theory(DisplayName = "Deve calcular corretamente os lucros e prejuízos de operações de day-trade sob ações.")]
        [MemberData(nameof(DayTradeData))]
        public void TestDayTrade(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocksResponse =
                response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal twentyPercentTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)stocksResponse.DayTradeProfit;

            if (stocksResponse.DayTradeProfit > 0)
                Assert.Equal((double)twentyPercentTaxes, stocksResponse.Taxes);
            else
                Assert.Equal(0, stocksResponse.Taxes);

            double totalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();
            Assert.Equal(totalSold, stocksResponse.TotalSold);
        }

        public static IEnumerable<object[]> DayTradeData()
        {
            List<Movement.EquitMovement> movements = new();
            int movementsQuantity = Random.Shared.Next(0, 100);

            int buy = Random.Shared.Next(20000, 25000);
            int sell = Random.Shared.Next(25001, 30000);

            for (int i = 0; i <= movementsQuantity; i++)
            {
                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), true));
                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), true));
            }

            // Primeiro cenário de testes sobre lucro.
            yield return new object[]
            {
                movements
            };

            movements.Clear();

            buy = Random.Shared.Next(25001, 30000);
            sell = Random.Shared.Next(20000, 25000);

            for (int i = 0; i <= movementsQuantity; i++)
            {
                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", buy, 1, buy, new DateTime(2023, 01, 01), true));
                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", sell, 1, sell, new DateTime(2023, 01, 01), true));
            }

            // Segundo cenário de testes sobre prejuízos.
            yield return new object[]
            {
                movements
            };
        }

        #endregion

        #region Swing-trade unit tests

        [Theory(DisplayName = "Deve calcular corretamente operações swing-trade em ações. Deve considerar lucro/prejuízo," +
            "limite de inseção, desdobramentos, agrupamentos e bonificações.")]
        [MemberData(nameof(SwingTradeData))]
        public void TestSwingTrade(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "1");

            AssetIncomeTaxes stocksResponse =
                response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal fifteenPercentTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocksResponse.SwingTradeProfit;
            double totalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();

            if (totalSold > AliquotConstants.LimitForStocksSelling && stocksResponse.SwingTradeProfit > 0)
                Assert.Equal((double)fifteenPercentTaxes, stocksResponse.Taxes);
            else
                Assert.Equal(0, stocksResponse.Taxes);

            Assert.Equal(totalSold, stocksResponse.TotalSold);
        }

        public static IEnumerable<object[]> SwingTradeData()
        {
            // Lucro em duas operações de compra e venda.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 25000, 1, 25000, new DateTime(2023, 01, 02))
                }
            };

            // Operações não ultrapassaram o limite de isenção e houve lucro.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 19653, 1, 19653, new DateTime(2023, 01, 02))
                }
            };

            // Operações ultrapassaram o limite de isenção, porém não houve lucro.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 22000, 1, 22000, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 22000, 1, 22000, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 22000, 1, 22000, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 21000, 1, 21000, new DateTime(2023, 01, 02))
                }
            };
        }

        #endregion

        #region Day-trade and swing-trade unit tests

        [Theory(DisplayName = "Deve calcular corretamenta operações de day-trade e swing-trade em ações. Ambas operações" +
            "devem ser calculadas separadamente. O imposto devido pelo investidor será a soma dos impostos dos dois tipos de operações.")]
        [MemberData(nameof(DayTradeAndSwingTradeData))]
        public void TestBothDayTradeAndSwingTrade(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "1");

            AssetIncomeTaxes stocksResponse =
                response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal swingTradeTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocksResponse.SwingTradeProfit;
            decimal dayTradeTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)stocksResponse.DayTradeProfit;

            double totalSold = movements.Where(x => x.MovementType.Equals(B3ResponseConstants.Sell)).Select(x => x.OperationValue).Sum();
            double expectedTaxes = 0;

            if (stocksResponse.SwingTradeProfit > 0) expectedTaxes += (double)swingTradeTaxes;
            if (stocksResponse.DayTradeProfit > 0) expectedTaxes += (double)dayTradeTaxes;

            Assert.Equal(totalSold, stocksResponse.TotalSold);
            Assert.Equal(expectedTaxes, stocksResponse.Taxes);
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
                    new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 26405, 1, 26405, new DateTime(2023, 01, 03)),
                    new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 27000, 1, 27000, new DateTime(2023, 01, 03), true)
                }
            };
        }

        #endregion
    }
}
