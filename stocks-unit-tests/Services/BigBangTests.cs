using stocks_common.Exceptions;
using stocks_core.Calculators;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Services.BigBang;
using stocks_unit_tests.Builders;

namespace stocks_unit_tests.Services
{
    public class BigBangTests
    {
        private readonly IBigBang bigBang;
        private IIncomeTaxesCalculator calculator;

        public BigBangTests()
        { 
            bigBang = new BigBang(calculator);
        }

        [Fact(DisplayName = "Se um investidor executar o Big Bang e não houver movimentos feitos anteriormente, uma exceção deve ser lançada.")]
        public void Should_throw_exception_if_investor_has_no_movements()
        {
            var emptyMovements = new Movement.Root();
            Assert.Throws<NoneMovementsException>(() => bigBang.Calculate(emptyMovements));
        }

        [Fact]
        public void Should_correctly_calculate_income_taxes_for_movements()
        {
            var movements = new MovementBuilder()
                .Create(B3ResponseConstants.Buy, 102, 1, B3ResponseConstants.Stocks, "PETR4")
                .Create(B3ResponseConstants.Buy, 154, 1, B3ResponseConstants.Stocks, "PETR4")
                .Create(B3ResponseConstants.Buy, 186, 1, B3ResponseConstants.Stocks, "PETR4")
                .Build();

            var response = bigBang.Calculate(movements);
        }

        public static IEnumerable<Movement.Root[]> IncomeTaxesData()
        {                        
            return new List<Movement.Root[]>()
            { 
                
            };
        }
    }
}
