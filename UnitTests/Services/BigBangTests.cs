using Common.Exceptions;
using Core.Calculators;
using Core.Models.B3;
using Core.Services.IncomeTaxes;
using Infrastructure.Dtos;
using Infrastructure.Repositories.AverageTradedPrice;
using Moq;
using System.Security.Policy;
using static Core.Models.B3.Movement;

namespace stocks_unit_tests.Services
{
    public class BigBangTests
    {
        private readonly IB3ResponseCalculatorService bigBang;

        private readonly IIncomeTaxesCalculator calculator;
        private readonly Mock<IAverageTradedPriceRepostory> repository;

        public BigBangTests()
        {
            repository = new Mock<IAverageTradedPriceRepostory>();

            bigBang = new B3ResponseCalculatorService(calculator, repository.Object);
        }

        [Fact(DisplayName = "Se um investidor executar o Big Bang e não houver movimentos feitos anteriormente, uma exceção deve ser lançada.")]
        public void ShouldThrowExceptionIfInvestorHasNoMovements()
        {
            var emptyMovements = new Movement.Root();
            Assert.ThrowsAsync<NotFoundException>(() => bigBang.Calculate(emptyMovements, It.IsAny<Guid>()));
        }

        [Theory(DisplayName = "Deve calcular corretamente todos os impostos de meses retroativos ao fazer a sincronização inicial da conta.")]
        [MemberData(nameof(BigBangData))]
        public async Task ShouldCalculateTaxesFromRetroactiveMonthsWhenSyncing(Root movement)
        {
            repository.Setup(x => x.GetAverageTradedPricesDto(It.IsAny<Guid>(), null)).ReturnsAsync(Array.Empty<AverageTradedPriceDto>());

            var result = await bigBang.Calculate(movement, It.IsAny<Guid>());

            double firstMonthTaxes = result!.Assets.Where(x => x.Month.Equals("01/2023")).Select(x => x.Taxes).Sum();
            double secondMonthTaxes = result!.Assets.Where(x => x.Month.Equals("02/2023")).Select(x => x.Taxes).Sum();

            Assert.Equal(540.55, firstMonthTaxes);
            Assert.Equal(31.9, secondMonthTaxes);
        }

        public static IEnumerable<object[]> BigBangData()
        {
            yield return new object[]
            {
                new Root
                {
                    Data = new Data
                    {
                        EquitiesPeriods = new EquitiesPeriods
                        {
                            EquitiesMovements = new List<EquitMovement>()
                            {
                                // Mês 01

                                // Lucro com PETR4. Como a operação abaixo ultrapassou 20k de vendas, há imposto de 15% a ser pago.
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 53, 1, 53, new DateTime(2023, 01, 01)),
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 64, 1, 64, new DateTime(2023, 01, 02)),
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 102, 1, 102, new DateTime(2023, 01, 03)),

                                // Lucro com VALE3 em day-trade, paga imposto de 20% sob lucro
                                new("VALE3", "Vale S.A.", "Ações", "Compra", 53, 1, 53, new DateTime(2023, 01, 01)),
                                new("VALE3", "Vale S.A.", "Ações", "Compra", 64, 1, 64, new DateTime(2023, 01, 03)),
                                new("VALE3", "Vale S.A.", "Ações", "Compra", 102, 1, 102, new DateTime(2023, 01, 03)),
                                new("VALE3", "Vale S.A.", "Ações", "Venda", 104, 1, 104, new DateTime(2023, 01, 03), true),

                                // Lucro com MUS3 com 20k > vendidos, paga imposto de 15% sob lucro
                                new("MUS3", "Music S.A.", "Ações", "Compra", 18320, 1, 18320, new DateTime(2023, 01, 10)),
                                new("MUS3", "Music S.A.", "Ações", "Venda", 21439, 1, 21439, new DateTime(2023, 01, 11)),

                                 // Lucro com CORP4 em day-trade, paga imposto de 20% sob lucro
                                new("CORP4", "Corporation S/A", "Ações", "Compra", 743, 1, 743, new DateTime(2023, 01, 16)),
                                new("CORP4", "Corporation S/A", "Ações", "Venda", 893, 1, 893, new DateTime(2023, 01, 16), true),

                                // Prejuízo com TETA4 em day-trade. Porém, como no final desse mês houve lucro ao invés de prejuízo, o imposto de 20% será aplicado
                                // nas operações day-trade.
                                new("TETA4", "Tetris S.A.", "Ações", "Compra", 20, 1, 20, new DateTime(2023, 01, 17)),
                                new("TETA4", "Tetris S.A.", "Ações", "Venda", 10, 1, 10, new DateTime(2023, 01, 17), true),

                                // Lucro com GOOGL, paga imposto de 20% sob lucro
                                new("GOOGL", "Alphabet Inc.", "BDR - Brazilian Depositary Receipts", "Compra", 743, 1, 743, new DateTime(2023, 01, 20)),
                                new("GOOGL", "Alphabet Inc.", "BDR - Brazilian Depositary Receipts", "Venda", 893, 1, 893, new DateTime(2023, 01, 21)),

                                // Mês 02

                                // Lucro com CPTS11, paga imposto de 20% sob lucro
                                new("CPTS11", "Capitania Securities II", "FII - Fundo de Investimento Imobiliário", "Compra", 201, 1, 201, new DateTime(2023, 02, 01)),
                                new("CPTS11", "Capitania Securities II", "FII - Fundo de Investimento Imobiliário", "Venda", 302, 1, 302, new DateTime(2023, 02, 02)),

                                // Lucro com BOVA11, paga imposto de 15% sob lucro
                                new("BOVA11", "Ibovespa", "ETF - Exchange Traded Fund", "Compra", 50, 1, 50, new DateTime(2023, 02, 01)),
                                new("BOVA11", "Ibovespa", "ETF - Exchange Traded Fund", "Venda", 72, 1, 72, new DateTime(2023, 02, 02)),

                                // Lucro com IVVB11 em day-trade, paga imposto de 20% sob lucro
                                new("IVVB11", "iShares S&P 500", "ETF - Exchange Traded Fund", "Compra", 50, 1, 50, new DateTime(2023, 02, 04)),
                                new("IVVB11", "iShares S&P 500", "ETF - Exchange Traded Fund", "Venda", 92, 1, 92, new DateTime(2023, 02, 04), true)
                            }
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
