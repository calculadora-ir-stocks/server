using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
using stocks_common.Exceptions;
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
            Assert.Throws<NoneMovementsException>(() => bigBang.Calculate(emptyMovements));
        }

        #region Swing-trade
        [Fact(DisplayName = "Deve calcular corretamente o imposto devido, total vendido e o lucro de swing-trade.")]
        public void Should_correctly_calculate_income_taxes_for_movements()
        {
            Root root = CreateMovements(
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 21573, 1, new DateTime(2023, 01, 01)),
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Buy, 22695, 1, new DateTime(2023, 01, 02)),
                new EquitMovement("PETR4", B3ResponseConstants.Stocks, B3ResponseConstants.Sell, 23211, 1, new DateTime(2023, 01, 03))
            );

            var response = bigBang.Calculate(root);

            foreach (var _ in response.Values)
            {
                foreach(var i in _)
                {
                    Assert.Equal(GetCalculatedTaxes(i.SwingTradeProfit, i.DayTradeProfit, AliquotConstants.IncomeTaxesForStocks), i.Taxes);
                    Assert.Equal(GetTotalSold(root), i.TotalSold);
                    Assert.Equal(1077, i.SwingTradeProfit);
                }
            }
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

            foreach(var i in equitiesMovements)
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

        private static double GetAverageTradedPrice(Root root, string tickerSymbol)
        {
            var buys = root.Data.EquitiesPeriods.EquitiesMovements.Where(
                x => x.MovementType == B3ResponseConstants.Buy &&
                x.TickerSymbol == tickerSymbol
            );

            double paid = buys.Select(x => x.OperationValue).Sum();
            int quantity = buys.Count();

            return paid / quantity;
        }

        private static double GetTotalSold(Root root)
        {
            return root.Data.EquitiesPeriods.EquitiesMovements.Where(x => x.MovementType == B3ResponseConstants.Sell).Select(x => x.OperationValue).Sum();
        }
    }
}
