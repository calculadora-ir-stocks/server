using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Core.Calculators;
using Core.Constants;
using Core.Services.IncomeTaxes;
using Infrastructure.Repositories.AverageTradedPrice;
using static Core.DTOs.B3.Movement;

namespace stocks_unit_tests.Services
{
    public class BigBangTests
    {
        private readonly IIncomeTaxesService bigBang;
        private readonly IIncomeTaxesCalculator calculator;
        private readonly IAverageTradedPriceRepostory repository;

        public BigBangTests()
        {
            bigBang = new IncomeTaxesService(calculator, repository);
        }

        [Fact(DisplayName = "Se um investidor executar o Big Bang e não houver movimentos feitos anteriormente, uma exceção deve ser lançada.")]
        public void Should_throw_exception_if_investor_has_no_movements()
        {
            var emptyMovements = new Root();
            Assert.ThrowsAsync<NoneMovementsException>(() => bigBang.GetB3ResponseDetails(emptyMovements, new Guid()));
        }
    }
}
