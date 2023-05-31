using stocks_common.Exceptions;
using stocks_core.Calculators;
using stocks_core.DTOs.B3;
using stocks_core.Services.BigBang;

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
    }
}
