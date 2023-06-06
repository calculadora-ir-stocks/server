using stocks_common.Exceptions;
using stocks_core.Calculators;
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

        [Theory(DisplayName = "Deve calcular corretamenta operações de day-trade e swing-trade em ações. Ambas operações" +
            "devem ser calculadas separadamente. O imposto devido pelo investidor será a soma dos impostos dos dois tipos de operações.")]
        [MemberData("SwingTradeAndDayTradeData")]
        public void Should_correctly_calculate_income_taxes_for_movements((Root, Result) request)
        {
            var response = bigBang.Calculate(request.Item1);

            foreach (var _ in response.Values)
            {
                foreach (var asset in _)
                {
                    Assert.Equal(asset.Taxes, request.Item2.Taxes);
                }
            }
        }
    }

    public class Action
    {
        public Action(string assetType, string tickerSymbol, string movementType, double value, int quantity, DateTime referenceDate)
        {
            AssetType = assetType;
            TickerSymbol = tickerSymbol;
            MovementType = movementType;
            Value = value;
            Quantity = quantity;
            ReferenceDate = referenceDate;
        }

        public string AssetType { get; set; }
        public string TickerSymbol { get; set; }
        public string MovementType { get; set; }
        public double Value { get; set; }
        public int Quantity { get; set; }
        public DateTime ReferenceDate { get; set; }
    }

    public class Result
    {
        public Result(double taxes, double swingTradeProfit, double dayTradeProfit, double averageTradedPrice)
        {
            Taxes = taxes;
            SwingTradeProfit = swingTradeProfit;
            DayTradeProfit = dayTradeProfit;
            AverageTradedPrice = averageTradedPrice;
        }

        public double Taxes { get; set; }
        public double SwingTradeProfit { get; set; }
        public double DayTradeProfit { get; set; }
        public double AverageTradedPrice { get; set; }
    }
}
