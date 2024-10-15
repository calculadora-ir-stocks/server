using Core.Constants;
using Core.Models;
using Core.Refit.B3;
using Hangfire.AverageTradedPriceUpdater;
using Hangfire.IncomeTaxesAdder;
using Infrastructure.Dtos;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.Taxes;
using Moq;
using System;

namespace stocks_unit_tests.Hangfire
{
    public class IncomeTaxesAdderHangfireTest
    {
        private readonly Mock<IAccountRepository> accountRepository;
        private readonly Mock<IIncomeTaxesRepository> incomeTaxesRepository;

        private readonly Mock<IB3Client> b3Client;
        private readonly Mock<CustomDateTime> customDateTime;

        public IncomeTaxesAdderHangfireTest()
        {
            this.accountRepository = new Mock<IAccountRepository>();
            this.incomeTaxesRepository = new Mock<IIncomeTaxesRepository>();
            this.b3Client = new Mock<IB3Client>();
            this.customDateTime = new Mock<CustomDateTime>();
        }

        [Theory(DisplayName = "Deve calcular e salvar o imposto de renda do mês anterior na base de dados. Deve, também, rodar esse job ANTES do job de " +
            "atualização de preço médio - logo, deve considerar o preço médio salvo na base de 2 meses atrás.")]
        [MemberData(nameof(DataSet))]
        public async Task ShouldAddUpdateAndDeleteCorrectly(Core.Models.B3.Movement.Root root, List<AverageTradedPriceDto> allTickers, Answer answer)
        {
            customDateTime.SetupGet(x => x.UtcNow).Returns(new DateTime(2024, 08, 01));
            int x = 10;
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
}
