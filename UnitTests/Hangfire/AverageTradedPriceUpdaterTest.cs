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

namespace stocks_unit_tests.Hangfire
{
    public class AverageTradedPriceUpdaterTest : ProfitCalculator
    {
        private readonly AverageTradedPriceUpdaterHangfire hangfire;

        private readonly Mock<IAverageTradedPriceRepostory> averageTradedPriceRepository;
        private readonly Mock<IAccountRepository> accountRepository;
        private readonly Mock<IB3Client> client;
        private readonly Mock<ILogger<AverageTradedPriceUpdaterHangfire>> logger;

        private readonly Fixture fixture = new();

        public AverageTradedPriceUpdaterTest()
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

        [Theory(DisplayName = "Deve obter corretamente os ativos a serem adicionados, atualizados e removidos na carteira de um investidor nas movimentações do último mês.")]
        [MemberData(nameof(DataSet))]
        public async Task ShouldAddUpdateAndDeleteCorrectly(Core.Models.B3.Movement.Root root, List<AverageTradedPriceDto> allTickers, Answer answer)
        {
            var movements = root.Data.EquitiesPeriods.EquitiesMovements;
            averageTradedPriceRepository.Setup(x => x.GetAverageTradedPrices(It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(allTickers);

            var allTickersThatWereNegotiated = await averageTradedPriceRepository.Object.GetAverageTradedPrices(new(), movements.Select(x => x.TickerSymbol).ToList());

            var tradedTickers = allTickersThatWereNegotiated
                .Select(x => new AverageTradedPriceDetails(x.Ticker, x.AverageTradedPrice, x.TotalBought, x.Quantity)).ToList();

            var _ = CalculateProfitAndAverageTradedPrice(root.Data.EquitiesPeriods.EquitiesMovements, tradedTickers);

            var tickersToAdd = AverageTradedPriceUpdaterHelper.GetTickersToAdd(tradedTickers, allTickers);
            var tickersToUpdate = AverageTradedPriceUpdaterHelper.GetTickersToUpdate(tradedTickers, allTickers);
            var tickersToRemove = AverageTradedPriceUpdaterHelper.GetTickersToRemove(tradedTickers, root.Data.EquitiesPeriods.EquitiesMovements);

            Assert.Equal(answer.TickersToAddQuantity, tickersToAdd.Count());
            Assert.Equal(answer.TickersToUpdateQuantity, tickersToUpdate.Count());
            Assert.Equal(answer.TickersToRemoveQuantity, tickersToRemove.Count());
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
                new Answer(1, 2, 2)
            };
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
                                // add
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
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.SellOperationType,
                                    OperationValue = 234.43,
                                    UnitPrice = 234.43,
                                    EquitiesQuantity = 2,
                                    ReferenceDate = new DateTime(2022, 02, 01)
                                }
                            }
                        }
                    }
                },
                new List<AverageTradedPriceDto>()
                {
                    new("BOVA11", 16.32, 16.32, 1, id),
                    new("AMER3", 146.32, 292.65, 2, id)
                },
                new Answer(2, 1, 1)
            };
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
                                    ProductTypeName = "ETF - Exchange Traded Fund",
                                    TickerSymbol = "BOVA11",
                                    CorporationName = "IVVB 11 Corporation Inc.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.SellOperationType,
                                    OperationValue = 12376.43,
                                    EquitiesQuantity = 6,
                                    ReferenceDate = new DateTime(2022, 01, 09)
                                }
                            }
                        }
                    }
                },
                new List<AverageTradedPriceDto>()
                {
                    new("BOVA11", 16.32, 16.32, 2, id),
                },
                new Answer(0, 0, 1)
            };
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
                                    ProductTypeName = "ETF - Exchange Traded Fund",
                                    TickerSymbol = "BOVA11",
                                    CorporationName = "IVVB 11 Corporation Inc.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 12376.43,
                                    EquitiesQuantity = 4,
                                    ReferenceDate = new DateTime(2022, 01, 09)
                                },
                                new()
                                {
                                    ProductTypeName = "ETF - Exchange Traded Fund",
                                    TickerSymbol = "BOVA11",
                                    CorporationName = "IVVB 11 Corporation Inc.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 12376.43,
                                    EquitiesQuantity = 6,
                                    ReferenceDate = new DateTime(2022, 01, 09)
                                },
                                new()
                                {
                                    ProductTypeName = "ETF - Exchange Traded Fund",
                                    TickerSymbol = "BOVA11",
                                    CorporationName = "IVVB 11 Corporation Inc.",
                                    MovementType = B3ResponseConstants.TransferenciaLiquidacao,
                                    OperationType = B3ResponseConstants.BuyOperationType,
                                    OperationValue = 12376.43,
                                    EquitiesQuantity = 1,
                                    ReferenceDate = new DateTime(2022, 01, 09)
                                }
                            }
                        }
                    }
                },
                new List<AverageTradedPriceDto>()
                {
                    new("BOVA11", 16.32, 16.32, 2, id),
                },
                new Answer(0, 1, 0)
            };
        }
    }

    public record Answer(int TickersToAddQuantity, int TickersToUpdateQuantity, int TickersToRemoveQuantity);
}
