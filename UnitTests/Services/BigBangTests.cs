using Common.Exceptions;
using Core.Calculators;
using Core.Models.B3;
using Core.Services.IncomeTaxes;
using Infrastructure.Repositories.AverageTradedPrice;

namespace stocks_unit_tests.Services
{
    public class BigBangTests
    {
        private readonly IB3ResponseCalculatorService bigBang;
        private readonly IIncomeTaxesCalculator calculator;
        private readonly IAverageTradedPriceRepostory repository;

        public BigBangTests()
        {
            bigBang = new B3ResponseCalculatorService(calculator, repository);
        }

        [Fact(DisplayName = "Se um investidor executar o Big Bang e não houver movimentos feitos anteriormente, uma exceção deve ser lançada.")]
        public void Should_throw_exception_if_investor_has_no_movements()
        {
            var emptyMovements = new Movement.Root();
            Assert.ThrowsAsync<NotFoundException>(() => bigBang.Calculate(emptyMovements, new Guid()));
        }
    }
}
