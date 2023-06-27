using stocks_common.Enums;
using stocks_common.Exceptions;
using stocks_common.Helpers;
using stocks_core.Calculators;
using stocks_core.Constants;
using stocks_core.Services.BigBang;
using static stocks_core.DTOs.B3.Movement;

namespace stocks_unit_tests.Services
{
    public class BigBangTests
    {
        private readonly IBigBang bigBang;
        private readonly IIncomeTaxesCalculator calculator;

        public BigBangTests()
        {
            bigBang = new BigBang(calculator);
        }

        [Fact(DisplayName = "Se um investidor executar o Big Bang e não houver movimentos feitos anteriormente, uma exceção deve ser lançada.")]
        public void Should_throw_exception_if_investor_has_no_movements()
        {
            var emptyMovements = new Root();
            Assert.Throws<NoneMovementsException>(() => bigBang.Execute(emptyMovements));
        }

        #region Swing-trade
        [Fact(DisplayName = "Deve calcular corretamente o imposto devido, total vendido e o lucro de swing-trade.")]
        public void SwingTradeWithProfitTradingStocks()
        {
            Root root = CreateMovements(
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 21573, 1, new DateTime(2023, 01, 01)),
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 22695, 1, new DateTime(2023, 01, 02)),
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Sell, 23211, 1, new DateTime(2023, 01, 03))
            );

            var response = bigBang.Execute(root);

            foreach (var i in response.Item1)
            {
                Assert.Equal(GetCalculatedTaxes(i.SwingTradeProfit, i.DayTradeProfit, AliquotConstants.IncomeTaxesForStocks), i.Taxes);
                Assert.Equal(GetTotalSold(root, "01/2023", i.AssetTypeId), i.TotalSold);
                Assert.Equal(1077, i.SwingTradeProfit);
            }
        }

        [Fact(DisplayName = "Deve calcular corretamente o imposto devido, total vendido e o prejuízo de swing-trade.")]
        public void SwingTradeWithLossTradingStocks()
        {
            Root root = CreateMovements(
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 21139, 1, new DateTime(2023, 01, 01)),
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 20394, 1, new DateTime(2023, 01, 02)),
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Sell, 20000, 1, new DateTime(2023, 01, 03))
            );

            var response = bigBang.Execute(root);

            foreach (var i in response.Item1)
            {
                Assert.Equal(GetCalculatedTaxes(i.SwingTradeProfit, i.DayTradeProfit, AliquotConstants.IncomeTaxesForStocks), i.Taxes);
                Assert.Equal(GetTotalSold(root, "01/2023", i.AssetTypeId), i.TotalSold);
                Assert.Equal(-766.5, i.SwingTradeProfit);
            }
        }

        [Fact(DisplayName = "Deve calcular corretamente o imposto devido, total vendido e o lucro de swing-trade com diferentes tipos de ações.")]
        public void SwingTradeWithProfitTradingMultipleStocks()
        {
            Root root = CreateMovements(
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 49000, 1, new DateTime(2023, 01, 03)),
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Sell, 52000, 1, new DateTime(2023, 01, 04)),
                new EquitMovement("VALE3", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 43000, 1, new DateTime(2023, 01, 10)),
                new EquitMovement("VALE3", B3ResponseConstants.Stocks, B3ResponseConstants.Sell, 48000, 1, new DateTime(2023, 01, 11))
            );

            var response = bigBang.Execute(root);

            foreach (var i in response.Item1)
            {
                Assert.Equal(GetCalculatedTaxes(i.SwingTradeProfit, i.DayTradeProfit, AliquotConstants.IncomeTaxesForStocks), i.Taxes);
                Assert.Equal(GetTotalSold(root, "01/2023", i.AssetTypeId), i.TotalSold);
            }
        }

        [Fact(DisplayName = "Deve calcular corretamente o imposto devido, total vendido e o lucro de swing-trade com diferentes tipos de ativos.")]
        public void SwingTradeWithProfitTradingMultipleAssets()
        {
            Root root = CreateMovements(
                new EquitMovement("GOOGL34", B3ResponseConstants.BDRs, B3ResponseConstants.Buy, 23.43, 1, new DateTime(2023, 02, 01)),
                new EquitMovement("AMZO34", B3ResponseConstants.BDRs, B3ResponseConstants.Buy, 56.44, 1, new DateTime(2023, 02, 01)),
                new EquitMovement("BOVA11", B3ResponseConstants.ETFs, B3ResponseConstants.Buy, 21043, 1, new DateTime(2023, 02, 02)),
                new EquitMovement("GOOGL34", B3ResponseConstants.BDRs, B3ResponseConstants.Buy, 25.54, 1, new DateTime(2023, 02, 03)),
                new EquitMovement("GOOGL34", B3ResponseConstants.BDRs, B3ResponseConstants.Sell, 26.54, 1, new DateTime(2023, 02, 03), dayTraded: true),
                new EquitMovement("AMZO34", B3ResponseConstants.BDRs, B3ResponseConstants.Buy, 59.76, 1, new DateTime(2023, 02, 04)),
                new EquitMovement("VALE3", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 25543, 1, new DateTime(2023, 02, 05)),
                new EquitMovement("VALE3", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 27654, 1, new DateTime(2023, 02, 06)),
                new EquitMovement("VALE3", B3ResponseConstants.Stocks, B3ResponseConstants.Sell, 24432, 1, new DateTime(2023, 02, 07))
            );

            var response = bigBang.Execute(root);
            //TO-DO
        }
        #endregion


        #region Day-trade

            #endregion

        private static Root CreateMovements(params EquitMovement[] equitiesMovements)
        {
            Root? response = new();
            response.Data = new();
            response.Data.EquitiesPeriods = new();
            response.Data.EquitiesPeriods.EquitiesMovements = new();

            foreach (var i in equitiesMovements)
            {
                response.Data.EquitiesPeriods.EquitiesMovements.Add(i);
            }

            return response;
        }

        private static double GetCalculatedTaxes(double swingTradeProfit, double dayTradeProfit, int aliquot)
        {
            decimal swingTradeTaxes = 0;
            decimal dayTradeTaxes = 0;

            if (swingTradeProfit > 0)
                swingTradeTaxes = (aliquot / 100m) * (decimal)swingTradeProfit;

            if (dayTradeProfit > 0)
                dayTradeTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)dayTradeProfit;

            decimal totalTaxes = swingTradeTaxes + dayTradeTaxes;

            return (double)totalTaxes;
        }

        private static double GetTotalSold(Root root, string date, Asset assetType)
        {
            return root.Data.EquitiesPeriods.EquitiesMovements.Where(
                x => x.MovementType == B3ResponseConstants.Sell && 
                x.ReferenceDate.ToString("MM/yyyy") == date &&
                x.AssetType == AssetTypeHelper.GetNameByAssetType(assetType)
            ).Select(x => x.OperationValue).Sum();
        }
    }
}
