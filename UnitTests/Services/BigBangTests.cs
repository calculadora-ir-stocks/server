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

        [Theory(DisplayName = "Deve calcular corretamente os impostos de movimentações que possuem mais de um ativo em uma operação de compra ou venda.")]
        [MemberData(nameof(TaxesFromMovementsWithMoreThanOneAssetPerOperationData))]
        public async Task ShouldCalculateTaxesFromMovementsWithMoreThanOneAssetPerOperation(Root movement)
        {
            var result = await bigBang.Calculate(movement, It.IsAny<Guid>());

            double taxes = result!.Assets.Where(x => x.Month.Equals("01/2023")).Select(x => x.Taxes).Sum();

            Assert.Equal(15.2, taxes);
        }

        [Theory(DisplayName = "Deve calcular corretamente todos os impostos de meses retroativos ao fazer a sincronização inicial da conta.")]
        [MemberData(nameof(BigBangData))]
        public async Task ShouldCalculateTaxesFromRetroactiveMonthsWhenSyncing(Root movement)
        {
            repository.Setup(x => x.GetAverageTradedPricesDto(It.IsAny<Guid>(), null)).ReturnsAsync(Array.Empty<AverageTradedPriceDto>());

            var result = await bigBang.Calculate(movement, It.IsAny<Guid>());

            double firstMonthTaxes = result!.Assets.Where(x => x.Month.Equals("01/2023")).Select(x => x.Taxes).Sum();
            double secondMonthTaxes = result!.Assets.Where(x => x.Month.Equals("02/2023")).Select(x => x.Taxes).Sum();

            Assert.Equal(531, Math.Round(firstMonthTaxes, 0));
            Assert.Equal(31.9, secondMonthTaxes);
        }

        [Theory(DisplayName = "Deve calcular corretamente todos os preços médios de meses retroativos ao fazer a sincronização inicial da conta.")]
        [MemberData(nameof(AverageTradedPriceData))]
        public async Task ShouldCalculateAverageTradedPricesWhenSyncing(Root movement)
        {
            repository.Setup(x => x.GetAverageTradedPricesDto(It.IsAny<Guid>(), It.IsAny<List<string>>())).ReturnsAsync(new List<AverageTradedPriceDto>
            {
                new("TETA4", 27, 54, 2)
            });

            var result = await bigBang.Calculate(movement, It.IsAny<Guid>());

            var averagePrices = result!.AverageTradedPrices.ToList();

            double petr4 = averagePrices.Where(x => x.TickerSymbol.Equals("PETR4")).Select(x => x.AverageTradedPrice).First();
            double vale3 = averagePrices.Where(x => x.TickerSymbol.Equals("VALE3")).Select(x => x.AverageTradedPrice).First();
            double mus3 = averagePrices.Where(x => x.TickerSymbol.Equals("MUS3")).Select(x => x.AverageTradedPrice).First();
            double teta4 = averagePrices.Where(x => x.TickerSymbol.Equals("TETA4")).Select(x => x.AverageTradedPrice).First();

            Assert.Equal(15, petr4);
            Assert.Equal(57.5, vale3);
            Assert.Equal(15760.5, mus3);
            Assert.Equal(54, teta4);
        }

        public static IEnumerable<object[]> TaxesFromMovementsWithMoreThanOneAssetPerOperationData()
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
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 128, 2, 64, new DateTime(2023, 01, 02), true),
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 204, 2, 102, new DateTime(2023, 01, 02)),
                            }
                        }
                    }
                }
            };
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

        public static IEnumerable<object[]> AverageTradedPriceData()
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
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 53, 1, 53, new DateTime(2023, 01, 01)),
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Compra", 64, 1, 64, new DateTime(2023, 01, 02)),
                                new("PETR4", "Petróleo Brasileiro S/A", "Ações", "Venda", 102, 1, 102, new DateTime(2023, 01, 03)),

                                new("VALE3", "Vale S.A.", "Ações", "Compra", 53, 1, 53, new DateTime(2023, 01, 01)),
                                new("VALE3", "Vale S.A.", "Ações", "Compra", 64, 1, 64, new DateTime(2023, 01, 03)),
                                new("VALE3", "Vale S.A.", "Ações", "Compra", 102, 1, 102, new DateTime(2023, 01, 03)),
                                new("VALE3", "Vale S.A.", "Ações", "Venda", 104, 1, 104, new DateTime(2023, 01, 03), true),

                                new("MUS3", "Music S.A.", "Ações", "Compra", 18320, 1, 18320, new DateTime(2023, 01, 10)),
                                new("MUS3", "Music S.A.", "Ações", "Compra", 34640, 2, 17320, new DateTime(2023, 01, 10)),
                                new("MUS3", "Music S.A.", "Ações", "Venda", 21439, 1, 21439, new DateTime(2023, 01, 11)),

                                new("TETA4", "Tetris S.A.", "Ações", "Compra", 20, 1, 20, new DateTime(2023, 01, 17)),
                                new("TETA4", "Tetris S.A.", "Ações", "Venda", 20, 2, 10, new DateTime(2023, 01, 17), true),
                            }
                        }
                    }
                }
            };
        }
    }    
}
