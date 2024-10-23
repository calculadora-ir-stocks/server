using AutoFixture;
using Core.Calculators;
using Core.Constants;
using Core.Models;
using Core.Refit.B3;
using Core.Services.Hangfire.AverageTradedPriceUpdater;
using Hangfire.AverageTradedPriceUpdater;
using Infrastructure.Dtos;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Microsoft.Extensions.Logging;
using Moq;

namespace stocks_unit_tests.Hangfire.Value
{
    public class AverageTradedPriceUpdaterValueTest : ProfitCalculator
    {
        private readonly AverageTradedPriceUpdaterHangfire hangfire;

        private readonly Mock<IAverageTradedPriceRepostory> averageTradedPriceRepository;
        private readonly Mock<IAccountRepository> accountRepository;
        private readonly Mock<IB3Client> client;
        private readonly Mock<ILogger<AverageTradedPriceUpdaterHangfire>> logger;

        private readonly Fixture fixture = new();

        public AverageTradedPriceUpdaterValueTest()
        {
            averageTradedPriceRepository = new Mock<IAverageTradedPriceRepostory>();
            accountRepository = new Mock<IAccountRepository>();
            client = new Mock<IB3Client>();
            logger = new Mock<ILogger<AverageTradedPriceUpdaterHangfire>>();

            hangfire = new AverageTradedPriceUpdaterHangfire(
                averageTradedPriceRepository.Object,
                accountRepository.Object,
                client.Object,
                logger.Object
            );
        }

        [Theory(DisplayName = "Deve obter corretamente as propriedades (preço médio, total gasto e quantidade) dos ativos a serem adicionados," +
            " atualizados e removidos na carteira de um investidor nas movimentações do último mês.")]
        [MemberData(nameof(DataSet))]
        public async Task ShouldAddUpdateAndDeleteCorrectly(Core.Models.B3.Movement.Root root, List<AverageTradedPriceDto> tickersInvestorAlreadyHas, IEnumerable<Answer> answers)
        {
            var movements = root.Data.EquitiesPeriods.EquitiesMovements;
            averageTradedPriceRepository.Setup(x => x.GetAverageTradedPrices(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(tickersInvestorAlreadyHas);

            var allTickersThatWereNegotiated = await averageTradedPriceRepository.Object.GetAverageTradedPrices(new(), movements.Select(x => x.TickerSymbol).ToList());

            var tradedTickers = allTickersThatWereNegotiated
                .Select(x => new AverageTradedPriceDetails(x.Ticker, x.AverageTradedPrice, x.TotalBought, x.Quantity)).ToList();

            var _ = CalculateProfitAndAverageTradedPrice(root.Data.EquitiesPeriods.EquitiesMovements, tradedTickers);

            var tickersToUpdate = AverageTradedPriceUpdaterHelper.GetTickersToUpdate(tradedTickers, tickersInvestorAlreadyHas);

            foreach (var ticker in tickersToUpdate)
            {
                var correctAnswerForThisTicker = answers.Where(x => x.Ticker.Equals(ticker)).First();
                var tickerToBeAsserted = tradedTickers.Where(x => x.TickerSymbol.Equals(correctAnswerForThisTicker.Ticker)).First();

                Assert.Equal(correctAnswerForThisTicker.AverageTradedPrice, tickerToBeAsserted.AverageTradedPrice);
                Assert.Equal(correctAnswerForThisTicker.TotalBought, tickerToBeAsserted.TotalBought);
                Assert.Equal(correctAnswerForThisTicker.Quantity, tickerToBeAsserted.TradedQuantity);
            }
        }

        public static IEnumerable<object[]> DataSet()
        {
            Guid id = new();

            yield return new object[]
            {
                new Core.Models.B3.Movement.Root()
                {
                    Data = new()
                    {
                        EquitiesPeriods = new()
                        {
                            EquitiesMovements = new()
                            {
                                // add
                                new()
                                {
                                    ProductTypeName = "FII - Fundo de Investimento Imobiliário",
                                    TickerSymbol = "KFOF11",
                                    CorporationName = "KFOF11 Corporation Inc.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 231.34,
                                    EquitiesQuantity = 1,
                                    ReferenceDate = new DateTime(2022, 01, 16)
                                },
                                // update
                                new()
                                {
                                    ProductTypeName = "ETF - Exchange Traded Fund",
                                    TickerSymbol = "IVVB11",
                                    CorporationName = "IVVB 11 Corporation Inc.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 12376.43,
                                    EquitiesQuantity = 4,
                                    ReferenceDate = new DateTime(2022, 01, 09)
                                },
                                // update
                                new()
                                {
                                    ProductTypeName = "ETF - Exchange Traded Fund",
                                    TickerSymbol = "BOVA11",
                                    CorporationName = "IVVB 11 Corporation Inc.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 12376.43,
                                    EquitiesQuantity = 4,
                                    ReferenceDate = new DateTime(2022, 01, 09)
                                },
                                // remove
                                new()
                                {
                                    ProductTypeName = "Ações",
                                    TickerSymbol = "AMER3",
                                    CorporationName = "Americanas S/A",
                                    OperationType = B3ResponseConstants.SellOperationType,
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationValue = 234.43,
                                    UnitPrice = 234.43,
                                    EquitiesQuantity = 2,
                                    ReferenceDate = new DateTime(2022, 02, 01)
                                },
                                // comprado e vendido na mesma operação. vai cair no tickersToRemove
                                new()
                                {
                                    ProductTypeName = "Ações",
                                    TickerSymbol = "DONT",
                                    CorporationName = "DONT DO ANYTHING",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 234.43,
                                    UnitPrice = 234.43,
                                    EquitiesQuantity = 1,
                                    ReferenceDate = new DateTime(2022, 02, 01)
                                },
                                new()
                                {
                                    ProductTypeName = "Ações",
                                    TickerSymbol = "DONT",
                                    CorporationName = "DONT DO ANYTHING",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.SellOperationType,
                                    OperationValue = 234.43,
                                    UnitPrice = 234.43,
                                    EquitiesQuantity = 1,
                                    ReferenceDate = new DateTime(2022, 02, 01)
                                }
                            }
                        }
                    }
                },
                new List<AverageTradedPriceDto>()
                {
                    new("BOVA11", 16.32, 16.32, 1, id),
                    new("IVVB11", 93.88, 187.76, 2, id),
                    new("AMER3", 146.32, 292.65, 2, id)
                },
                new List<Answer>()
                {
                    new("BOVA11", 2478.55, 12392.75, 5),
                    new("IVVB11", 2094.0316666666668, 12564.19, 6)
                }
            };

            // Investidor tem o ativo na carteira, compra mais uma vez e depois o vende.
            yield return new object[]
            {
                new Core.Models.B3.Movement.Root()
                {
                    Data = new()
                    {
                        EquitiesPeriods = new()
                        {
                            EquitiesMovements = new()
                            {
                                new()
                                {
                                    ProductTypeName = "Ação",
                                    TickerSymbol = "PETR4",
                                    CorporationName = "Petrobras",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 28.65,
                                    EquitiesQuantity = 100,
                                    ReferenceDate = new DateTime(2022, 02, 01)
                                },
                                new()
                                {
                                    ProductTypeName = "Ação",
                                    TickerSymbol = "PETR4",
                                    CorporationName = "Petrobras",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.SellOperationType,
                                    OperationValue = 30.10,
                                    EquitiesQuantity = 50,
                                    ReferenceDate = new DateTime(2022, 02, 05)
                                },
                                new()
                                {
                                    ProductTypeName = "FII - Fundo de Investimento Imobiliário",
                                    TickerSymbol = "HGLG11",
                                    CorporationName = "CSHG Logística",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 165.50,
                                    EquitiesQuantity = 10,
                                    ReferenceDate = new DateTime(2022, 03, 01)
                                },
                                new()
                                {
                                    ProductTypeName = "Ação",
                                    TickerSymbol = "VALE3",
                                    CorporationName = "Vale S.A.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 78.45,
                                    EquitiesQuantity = 200,
                                    ReferenceDate = new DateTime(2022, 04, 10)
                                },
                                new()
                                {
                                    ProductTypeName = "Ação",
                                    TickerSymbol = "VALE3",
                                    CorporationName = "Vale S.A.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.SellOperationType,
                                    OperationValue = 82.50,
                                    EquitiesQuantity = 100,
                                    ReferenceDate = new DateTime(2022, 04, 15)
                                },
                                new()
                                {
                                    ProductTypeName = "Ação",
                                    TickerSymbol = "MGLU3",
                                    CorporationName = "Magazine Luiza S.A.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 4.32,
                                    EquitiesQuantity = 500,
                                    ReferenceDate = new DateTime(2022, 05, 01)
                                },
                                new()
                                {
                                    ProductTypeName = "Ação",
                                    TickerSymbol = "MGLU3",
                                    CorporationName = "Magazine Luiza S.A.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.SellOperationType,
                                    OperationValue = 4.50,
                                    EquitiesQuantity = 200,
                                    ReferenceDate = new DateTime(2022, 05, 05)
                                }
                            }
                        }
                    }
                },
                new List<AverageTradedPriceDto>()
                {
                    new("TSLA11", 1469.75, 5879, 4, id)
                },
                new List<Answer>()
                {
                    new("TSLA11", 1222.068, 6110.34, 3)
                }
            };
        }
    }

    public record Answer(string Ticker, double AverageTradedPrice, double TotalBought, int Quantity);
}
