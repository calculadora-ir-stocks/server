using Common.Exceptions;
using Core.Calculators;
using Core.Models.B3;
using Core.Services.IncomeTaxes;
using Infrastructure.Repositories.AverageTradedPrice;
using Moq;
using System.Security.Policy;
using static Core.Models.B3.Movement;

namespace stocks_unit_tests.Services
{
    public class BigBangTests
    {
        private readonly IB3ResponseCalculatorService bigBang;

        private readonly Mock<IIncomeTaxesCalculator> calculator;
        private readonly Mock<IAverageTradedPriceRepostory> repository;

        public BigBangTests()
        {
            calculator = new Mock<IIncomeTaxesCalculator>();
            repository = new Mock<IAverageTradedPriceRepostory>();

            bigBang = new B3ResponseCalculatorService(calculator.Object, repository.Object);
        }

        [Fact(DisplayName = "Se um investidor executar o Big Bang e não houver movimentos feitos anteriormente, uma exceção deve ser lançada.")]
        public void ShouldThrowExceptionIfInvestorHasNoMovements()
        {
            var emptyMovements = new Movement.Root();
            Assert.ThrowsAsync<NotFoundException>(() => bigBang.Calculate(emptyMovements, It.IsAny<Guid>()));
        }

        [Theory(DisplayName = "Deve calcular corretamente todos os impostos de meses retroativos ao fazer a sincronização inicial da conta.")]
        [MemberData(nameof(BigBangData))]
        public void ShouldCalculateTaxesFromRetroactiveMonthsWhenSyncing(Root movement)
        {
            bigBang.Calculate(movement, It.IsAny<Guid>());
            throw new NotImplementedException();
        }

        public static IEnumerable<object[]> BigBangData()
        {
            yield return new object[]
            {
                // TODO
                new Root
                {
                    Data = new Data
                    {
                        EquitiesPeriods = new EquitiesPeriods
                        {
                            EquitiesMovements = new List<EquitMovement>()
                        }
                    }
                }
            };
        }

        [Fact(DisplayName = "Deve calcular corretamente todos os preços médios de meses retroativos ao fazer a sincronização inicial da conta.")]
        public void ShouldCalculateAverageTradedPricesWhenSyncing()
        {
            throw new NotImplementedException();
        }
    }
}
