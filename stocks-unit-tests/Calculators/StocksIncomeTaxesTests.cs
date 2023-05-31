using stocks_core.Calculators;
using stocks_core.Calculators.Assets;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_unit_tests.Business
{
    public class StocksIncomeTaxesTests
    {
        private readonly IIncomeTaxesCalculator stocksCalculator;

        public StocksIncomeTaxesTests()
        {
            stocksCalculator = new StocksIncomeTaxes();
        }

        [Fact(DisplayName = "Caso uma operação de compra e venda sejam feitas no mesmo dia, deve ser considerada day-trade" +
            "e a alíquota sob o lucro líquido deverá ser de 20%.")]
        public void Should_be_day_trade_if_buy_and_sell_are_on_the_same_day()
        {
            List<AssetIncomeTaxes> response = new();

            List<Movement.EquitMovement> movements = new()
            {
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 25000, 1, 25000, new DateTime(2023, 01, 01))
            };

            stocksCalculator.CalculateIncomeTaxesForSpecifiedMovements(response, movements);

            double profit = movements[1].OperationValue - movements[0].OperationValue;
            decimal twentyPercentTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal) profit;

            Assert.True(response[0].DayTraded);
            Assert.Equal(response[0].Taxes, (double)twentyPercentTaxes);
        }

        [Fact(DisplayName = "Caso uma operação de compra e venda sejam feitas em dias diferentes, deve ser considerada swing-trade" +
            "e a alíquota sob o lucro líquido deverá ser de 15%.")]
        public void Should_be_swing_trade_if_buy_and_sell_are_on_different_days()
        {
            List<AssetIncomeTaxes> response = new();

            List<Movement.EquitMovement> movements = new()
            {
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 25000, 1, 25000, new DateTime(2023, 01, 02))
            };

            stocksCalculator.CalculateIncomeTaxesForSpecifiedMovements(response, movements);

            double profit = movements[1].OperationValue - movements[0].OperationValue;
            decimal fifteenPercentTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)profit;

            Assert.False(response[0].DayTraded);
            Assert.Equal(response[0].Taxes, (double)fifteenPercentTaxes);
        }

        [Fact(DisplayName = "Caso as operações de venda de um mês não ultrapassem o valor de R$20.000, o investidor não terá que" +
            "pagar imposto de renda.")]
        public void Should_not_pay_taxes_if_month_sells_are_less_than_20000()
        {
            List<AssetIncomeTaxes> response = new();

            List<Movement.EquitMovement> movements = new()
            {
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21405, 1, 21405, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 19653, 1, 19653, new DateTime(2023, 01, 02))
            };

            stocksCalculator.CalculateIncomeTaxesForSpecifiedMovements(response, movements);

            Assert.Equal(0, response[0].Taxes);
        }

        [Fact(DisplayName = "Caso as operações de venda de um mês ultrapassem o limite de R$20.000 mas houveram prejuízos, o investidor não" +
            "terá que pagar imposto de renda.")]
        public void Should_not_pay_taxes_if_loss_happened_even_if_investor_sold_more_than_20000()
        {
            List<AssetIncomeTaxes> response = new();

            List<Movement.EquitMovement> movements = new()
            {
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 22000, 1, 22000, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 22000, 1, 22000, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 22000, 1, 22000, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 21000, 1, 21000, new DateTime(2023, 01, 02))
            };

            stocksCalculator.CalculateIncomeTaxesForSpecifiedMovements(response, movements);

            Assert.Equal(0, response[0].Taxes);
            // Prejuízo de R$1.000
            Assert.Equal(-1000, response[0].SwingTradeProfit);
        }

        [Fact(DisplayName = "Caso o investidor tenha feito day-trade, mas houveram prejuízos, o investidor não terá que pagar imposto de renda.")]
        public void Should_not_pay_taxes_if_loss_happened_even_if_investor_day_traded()
        {
            List<AssetIncomeTaxes> response = new();

            List<Movement.EquitMovement> movements = new()
            {
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 21000, 1, 21000, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 20000, 1, 20000, new DateTime(2023, 01, 02))
            };

            stocksCalculator.CalculateIncomeTaxesForSpecifiedMovements(response, movements);

            Assert.Equal(0, response[0].Taxes);
            // Prejuízo de R$1.000
            Assert.Equal(-1000, response[0].SwingTradeProfit);
        }

        [Fact(DisplayName = "Caso o investidor tenha vendido duas ações diferentes em um mês e tenha ultrapassado o limite de R$20.000," +
            "deverá pagar o imposto de renda sob o lucro das duas operações.")]
        public void Should_pay_taxes_when_two_different_tickers_are_sold()
        {
            List<AssetIncomeTaxes> response = new();

            List<Movement.EquitMovement> movements = new()
            {
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 20000, 1, 20000, new DateTime(2023, 01, 01)),
                new Movement.EquitMovement("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 21000, 1, 21000, new DateTime(2023, 01, 02)),
                new Movement.EquitMovement("VALE3", "Vale S.A.", "Ações", "Compra", 10, 1, 10, new DateTime(2023, 01, 03)),
                new Movement.EquitMovement("VALE3", "Vale S.A.", "Ações", "Venda", 12, 1, 12, new DateTime(2023, 01, 04)),
            };

            stocksCalculator.CalculateIncomeTaxesForSpecifiedMovements(response, movements);

            var petr4Profit = movements[1].OperationValue - movements[0].OperationValue;
            var vale3Profit = movements[3].OperationValue - movements[2].OperationValue;

            var totalProfit = petr4Profit + vale3Profit;
            decimal tax = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)totalProfit;

            Assert.Equal((double)tax, response[0].Taxes);
        }
    }
}
