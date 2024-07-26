using Core.Calculators;
using Core.Constants;
using Core.Models;
using Core.Models.B3;
using Core.Services.B3ResponseCalculator;
using Infrastructure.Dtos;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.BonusShare;
using Microsoft.Extensions.Logging;
using Moq;

namespace stocks_unit_tests.Calculators
{
    /// <summary>
    /// Testes relacionados a Desdobramento, Grupamento e Bonificações de ativos.
    /// </summary>
    public class SpecialMovementsTests : ProfitCalculator
    {
        private readonly IB3ResponseCalculatorService calculator;
        private readonly IIncomeTaxesCalculator incomeTaxesCalculator = null!;

        private readonly Mock<IAverageTradedPriceRepostory> averageTradedPriceRepository;
        private readonly Mock<IBonusShareRepository> bonusShareRepository;
        private readonly Mock<ILogger<B3ResponseCalculatorService>> logger;

        public SpecialMovementsTests()
        {
            averageTradedPriceRepository = new Mock<IAverageTradedPriceRepostory>();
            bonusShareRepository = new Mock<IBonusShareRepository>();
            logger = new Mock<ILogger<B3ResponseCalculatorService>>();

            calculator = new B3ResponseCalculatorService(
                incomeTaxesCalculator,
                averageTradedPriceRepository.Object,
                bonusShareRepository.Object,
                logger.Object);
        }

        #region Ativos comprados antes de 01/11/2019
        [Theory(DisplayName = "A API deve adicionar em uma lista todos os ativos que foram comprados antes de 01/11/2019.")]
        [MemberData(nameof(AssetsBoughtBeforeB3MinimumRange))]
        public void ShouldNotThrowUnexpectedExceptionIfInvestourBoughtAssetBeforeB3MinimumDateRange(List<Movement.EquitMovement> movements, int assetsBoughtBeforeB3MinimumRangeDate)
        {
            var response = CalculateProfitAndAverageTradedPrice(movements, new List<AverageTradedPriceDetails>());
            Assert.Equal(assetsBoughtBeforeB3MinimumRangeDate, response.TickersBoughtBeforeB3Range.Count);
        }

        public static IEnumerable<object[]> AssetsBoughtBeforeB3MinimumRange()
        {
            // Lucro em operações de swing-trade e day-trade, mas < 20k foram vendidos. Nesse caso, apenas 20% devem ser aplicados nas operações
            // de day-trade.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 7653, 1, 7653, new DateTime(2023, 01, 03), true),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 8653, 1, 8653, new DateTime(2023, 01, 03), true),
                    new("MGLU3", "Magazine Luiza", "Ações", B3ResponseConstants.Split, string.Empty, 5467, 1, 5467, new DateTime(2019, 01, 01)),
                    new("MGLU3", "Magazine Luiza", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 6587, 1, 6587, new DateTime(2023, 01, 02))
                }, 1
            };
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("TSLA34", "Tesla Inc.", B3ResponseConstants.BDRs, B3ResponseConstants.ReverseSplit, string.Empty, 5467, 1, 5467, new DateTime(2019, 01, 01)),
                    new("AMER3", "Americanas Ltda", B3ResponseConstants.Stocks, B3ResponseConstants.BonusShare, string.Empty, 5467, 1, 5467, new DateTime(2020, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 7653, 1, 7653, new DateTime(2023, 01, 03), true),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 8653, 1, 8653, new DateTime(2023, 01, 03), true),
                    new("MGLU3", "Magazine Luiza", "Ações", B3ResponseConstants.Split, string.Empty, 5467, 1, 5467, new DateTime(2019, 01, 01)),
                    new("MGLU3", "Magazine Luiza", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 6587, 1, 6587, new DateTime(2023, 01, 02))
                }, 3
            };
        }
        #endregion

        #region Desdobramentos

        // É importante destacar que o 'split' nesse caso está representando o retorno da B3 que, conforme alinhado, retornará QUANTOS ativos
        // o investidor receberá na carteira após o desdobramento.

        // Considerando o primeiro exemplo onde o investidor possui 10 ações compradas por 1000, a B3 retornará 30 pois já sabe que o investidor possui 10 ações - por mais
        // que essa informação NÃO SEJA retornada por eles, e sim calculada manualmente pelo Stocks no momento da sincronização da conta.

        // Dito isso, a calculadora não precisa multiplicar a quantidade atual de ativos pelo desdobramento pois isso já é feito pela B3.
        [Theory(DisplayName = "Deve calcular corretamente uma operação de desdobramento ao somar os ativos que foram adicionados à carteira " +
            "com a quantidade de ativos atuais. Depois, deve ser feito o cálculo do preço médio com base na nova alteração.")]
        [InlineData(1000, 10, 30)] // Feito um split de 1 pra 4. 1000 / (10 + 30) = 25
        [InlineData(72, 6, 12)] // Feito um split de 1 pra 3. 72 / (6 + 12) = 4
        [InlineData(100.43, 10, 10)]
        [InlineData(82.32, 100, 500)]
        public void ShouldCalculateSplitOperationsCorrectly(double totalBought, int quantity, int split)
        {
            List<AverageTradedPriceDetails> averageTradedPrices = new()
            {
                new("PETR4", totalBought / quantity, totalBought, quantity),
                new("GOOGL34", 0, 0, 0),
                new("ITSA4", 0, 0, 0)
            };

            List<Movement.EquitMovement> movement = new()
            {
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.Split, string.Empty, 0, split, 0, new DateTime(2023, 01, 01), false),
            };

            CalculateProfitAndAverageTradedPrice(movement, averageTradedPrices);

            var expectedPrice = totalBought / (quantity + movement.First().EquitiesQuantity);

            Assert.Equal(expectedPrice, averageTradedPrices.First().AverageTradedPrice);
        }
        #endregion

        #region Grupamentos

        // É importante destacar que o 'newQuantityInPortfolio' nesse caso está representando o retorno da B3 que, conforme alinhado, retornará A NOVA posição do ativo
        // do investidor após o grupamento.

        // Considerando o primeiro exemplo onde o investidor possui 6 ações compradas por 72 e um cenário de grupamento 2 por 1 (ou seja, 2 ativos viram 1),
        // a B3 retornará 3 pois 6 / 2 = 3.

        // Dito isso, não há a necessidade de calcular nada, simplesmente dividir o total comprado pela nova posição do investidor.
        [Theory(DisplayName = "Deve calcular corretamente uma operação de grupamento ao dividir o total comprado " +
            "com a nova quantidade de ativos na posição atual. Depois, deve ser feito o cálculo do preço médio com base na nova alteração.")]
        [InlineData(72, 6, 3)]
        [InlineData(92.32, 20, 4)]
        [InlineData(23.43, 50, 5)]
        public void ShouldCalculateReverseSplitOperationsCorrectly(double totalBought, int quantity, int newQuantityInPortfolio)
        {
            List<AverageTradedPriceDetails> averageTradedPrices = new()
            {
                new("PETR4", totalBought / quantity, totalBought, quantity),
                new("GOOGL34", 0, 0, 0),
                new("ITSA4", 0, 0, 0)
            };

            List<Movement.EquitMovement> movement = new()
            {
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.ReverseSplit, string.Empty, 0, newQuantityInPortfolio, 0, new DateTime(2023, 01, 01), false),
            };

            CalculateProfitAndAverageTradedPrice(movement, averageTradedPrices);

            var expectedPrice = totalBought / newQuantityInPortfolio;

            Assert.Equal(expectedPrice, averageTradedPrices.First().AverageTradedPrice);
        }
        #endregion

        #region Bonificações
        // É importante destacar que o 'bonusShareQuantity' nesse caso está representando o retorno da B3 que, conforme alinhado, retornará a quantidade de ativos bonificados
        // para o investidor após a bonificação.

        // Considerando o primeiro exemplo onde o investidor possui 6 ações compradas por 72 e um cenário de grupamento 2 por 1 (ou seja, 2 ativos viram 1),
        // a B3 retornará 3 pois 6 / 2 = 3.

        // Dito isso, não há a necessidade de calcular nada, simplesmente dividir o total comprado pela nova posição do investidor.
        [Theory(DisplayName = "Deve calcular corretamente uma operação de bonificação ao somar a nova quantidade de ativos e total pago pelo ativo. Depois, " +
            "o preço médio deve ser calculado com base nesses novos valores.")]
        [InlineData(1000, 100, 10, 40)]
        public void ShouldCalculateBonusShareOperationsCorrectly(double totalBought, int quantity, int bonusShareQuantity, int bonusShareOperationValue)
        {
            List<AverageTradedPriceDetails> averageTradedPrices = new()
            {
                new("PETR4", totalBought / quantity, totalBought, quantity),
                new("GOOGL34", 0, 0, 0),
                new("ITSA4", 0, 0, 0)
            };

            List<Movement.EquitMovement> movement = new()
            {
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.BonusShare, string.Empty, bonusShareOperationValue, bonusShareQuantity, 0, new DateTime(2023, 01, 01), false),
            };

            CalculateProfitAndAverageTradedPrice(movement, averageTradedPrices);
            Assert.Equal(9.09, Convert.ToDouble(averageTradedPrices.First().AverageTradedPrice.ToString("00.00")));
        }
        #endregion

        #region Preço médio em operações de compra, venda, desdobramentos, grupamentos e bonificações 
        [Theory(DisplayName = "Deve calcular corretamente o preço médio após operações de desdobramento, grupamentos e bonificações.")]
        [MemberData(nameof(AllOperationsDataSet))]
        public async Task TestAverageTradedPricesAfterSpecialMovements(List<Movement.EquitMovement> movements)
        {
            bonusShareRepository.Setup(x => x.GetByTickerAndDate("PETR4", It.IsAny<DateTime>()))
                .ReturnsAsync(new Infrastructure.Models.BonusShare("PETR4", new DateTime(), 50, 0));

            bonusShareRepository.Setup(x => x.GetByTickerAndDate("BLMO11", It.IsAny<DateTime>()))
                .ReturnsAsync(new Infrastructure.Models.BonusShare("BLMO11", new DateTime(), 20, 0));

            var root = new Movement.Root
            {
                Data = new Movement.Data
                {
                    EquitiesPeriods = new Movement.EquitiesPeriods
                    {
                        EquitiesMovements = movements
                    }
                }
            };

            var response = await calculator.Calculate(root, It.IsAny<Guid>());
            var averageTradedPrices = response!.AverageTradedPrices;

            var petr4 = averageTradedPrices.Where(x => x.TickerSymbol.Equals("PETR4")).First();
            var googl34 = averageTradedPrices.Where(x => x.TickerSymbol.Equals("GOOGL34")).First();
            var nvdc34 = averageTradedPrices.Where(x => x.TickerSymbol.Equals("NVDC34")).First();
            var blmo11 = averageTradedPrices.Where(x => x.TickerSymbol.Equals("BLMO11")).First();

            Assert.Equal(2101.17, Convert.ToDouble(petr4.AverageTradedPrice.ToString("0000.00")));
            Assert.Equal(113.33, Convert.ToDouble(googl34.AverageTradedPrice.ToString("000.00")));
            Assert.Equal(104, nvdc34.AverageTradedPrice);
            Assert.Equal(18.57, Convert.ToDouble(blmo11.AverageTradedPrice.ToString("00.00")));
        }
        #endregion

        #region Impostos a serem pagos em operações de compra, venda, desdobramentos, grupamentos e bonificações 
        [Theory(DisplayName = "Deve calcular corretamente o imposto após operações de desdobramento, grupamentos e bonificações.")]
        [MemberData(nameof(AllOperationsDataSet))]
        public async Task TestIncomeTaxesAfterSpecialMovements(List<Movement.EquitMovement> movements)
        {
            averageTradedPriceRepository.Setup(x => x.GetAverageTradedPrices(It.IsAny<Guid>(), null)).ReturnsAsync(Array.Empty<AverageTradedPriceDto>());

            bonusShareRepository.Setup(x => x.GetByTickerAndDate("PETR4", It.IsAny<DateTime>()))
                .ReturnsAsync(new Infrastructure.Models.BonusShare("PETR4", new DateTime(), 50, 0));

            bonusShareRepository.Setup(x => x.GetByTickerAndDate("BLMO11", It.IsAny<DateTime>()))
                .ReturnsAsync(new Infrastructure.Models.BonusShare("BLMO11", new DateTime(), 20, 0));

            var root = new Movement.Root
            {
                Data = new Movement.Data
                {
                    EquitiesPeriods = new Movement.EquitiesPeriods
                    {
                        EquitiesMovements = movements
                    }
                }
            };
            var response = await calculator.Calculate(root, It.IsAny<Guid>());

            var month1 = response!.Assets.Where(x => x.Month.Equals("01/2023")).First();
            var month3 = response.Assets.Where(x => x.Month.Equals("03/2023")).First();

            var month3TaxesFormatted = Convert.ToDouble((Math.Truncate(month3.Taxes * 100) / 100).ToString("00.00")); // month3.Taxes é uma dízima não periódica. Aqui é obtido seu valor no formato 00.00
            // sem arredondar.

            Assert.Equal(224, month1.Taxes);
            Assert.Equal(15.33, month3TaxesFormatted);
        }

        [Theory(DisplayName = "Deve calcular corretamente o imposto após operações de desdobramento, grupamentos e bonificações (pt. 2).")]
        [MemberData(nameof(Part2DataSet))]
        public async Task TestIncomeTaxesAfterSpecialMovementsPart2(List<Movement.EquitMovement> movements)
        {
            averageTradedPriceRepository.Setup(x => x.GetAverageTradedPrices(It.IsAny<Guid>(), null)).ReturnsAsync(Array.Empty<AverageTradedPriceDto>());

            var root = new Movement.Root
            {
                Data = new Movement.Data
                {
                    EquitiesPeriods = new Movement.EquitiesPeriods
                    {
                        EquitiesMovements = movements
                    }
                }
            };

            var response = await calculator.Calculate(root, It.IsAny<Guid>());

            var month01 = response!.Assets.Where(x => x.Month.Equals("01/2023")).First();
            var month02 = response.Assets.Where(x => x.Month.Equals("02/2023")).First();
            var month03 = response.Assets.Where(x => x.Month.Equals("03/2023")).First();
            var month04 = response.Assets.Where(x => x.Month.Equals("04/2023")).First();
            var month05 = response.Assets.Where(x => x.Month.Equals("05/2023")).First();
            var month06 = response.Assets.Where(x => x.Month.Equals("06/2023")).First();
            var month07 = response.Assets.Where(x => x.Month.Equals("07/2023")).First();
            var month08 = response.Assets.Where(x => x.Month.Equals("08/2023")).First();

            Assert.Equal(2, month01.Taxes);
            Assert.Equal(0, month02.Taxes);
            Assert.Equal(22.275, month03.Taxes);
            Assert.Equal(30, month04.Taxes);
            Assert.Equal(0, month05.Taxes);
            Assert.Equal(15, month06.Taxes);
            Assert.Equal(0, month07.Taxes);
            Assert.Equal(15, Convert.ToDouble(month08.Taxes.ToString("00.00")));
        }

        public static IEnumerable<object[]> Part2DataSet()
        {
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    // Mês 01
                    // Lucro day-trade: 10
                    // IR: 2
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 120, 1, 120, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 100, 1, 100, new DateTime(2023, 01, 01), true),
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 150, 1, 150, new DateTime(2023, 01, 03)),
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 180, 1, 180, new DateTime(2023, 01, 03), true),

                    // Mês 02
                    // IR: 0, teve prejuízo
                    // Preço médio 75,75
                    new("GOOGL34", "Google LLC", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 100, 2, 50, new DateTime(2023, 02, 01)),
                    new("GOOGL34", "Google LLC", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 203, 2, 101.50, new DateTime(2023, 02, 01)),
                    new("GOOGL34", "Google LLC", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 30, 2, 15, new DateTime(2023, 02, 01), true),

                    // Mês 03
                    // Lucro swing-trade: 148,5
                    // IR: 22,275
                    new("GOOGL34", "Google LLC", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 300, 2, 150, new DateTime(2023, 03, 01)),

                    // Mês 04
                    // IR: 20, teve prejuízo de -50 em day-trade, porém teve lucro de 150 em swing-trade. 20% no swing-trade por ser FII
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 400, 2, 200, new DateTime(2023, 04, 01)),
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 150, 1, 150, new DateTime(2023, 04, 01), true),
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 350, 1, 350, new DateTime(2023, 04, 03)),

                    // Mês 05
                    // IR: 0, teve prejuízo. Burro
                    new("BOVA11", "Ibovespa", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 600, 3, 200, new DateTime(2023, 05, 01)),
                    new("BOVA11", "Ibovespa", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 100, 1, 100, new DateTime(2023, 05, 02)),

                    // Mês 06
                    // Lucro swing-trade: 100
                    // IR: 15
                    new("BOVA11", "Ibovespa", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 300, 1, 300, new DateTime(2023, 06, 01)),
                    new("BOVA11", "Ibovespa", B3ResponseConstants.ETFs, B3ResponseConstants.Split, string.Empty, 0, 2, 0, new DateTime(2023, 06, 20)), // Split 1 pra 3.

                    // Mês 07
                    // Lucro swing-trade: -50
                    // IR: 0
                    new("BOVA11", "Ibovespa", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 150, 1, 150, new DateTime(2023, 07, 20)),

                    // Mês 08
                    // Lucro day-trade: 0 (ganhou 50 (150 - 100) e perdeu 50 (50-100)).
                    // Lucro swing-trade: 100
                    // IR: 16,11
                    new("IVVB11", "iShares S&P 500", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 1000, 10, 100, new DateTime(2023, 08, 01)),
                    new("IVVB11", "iShares S&P 500", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 150, 1, 150, new DateTime(2023, 08, 01), true),
                    new("IVVB11", "iShares S&P 500", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 50, 1, 50, new DateTime(2023, 08, 01), true),
                    new("IVVB11", "iShares S&P 500", B3ResponseConstants.ETFs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 200, 1, 200, new DateTime(2023, 08, 02)),
                }
            };
        }

        #endregion

        public static IEnumerable<object[]> AllOperationsDataSet()
        {
            // Detalhe: nas operações de bonificações, o UnitPrice e OperationValue não estão sendo passados, pois esses valores são definidos manualmente
            // através de uma consulta na base. A B3 não informa esses valores, então usamos um provedor terceiro para obtê-los.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    // Mês 01
                    // Lucro day-trade: 1.120
                    // IR: 224 
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 5467, 1, 5467, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 6587, 1, 6587, new DateTime(2023, 01, 01), true),
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 7653, 1, 7653, new DateTime(2023, 01, 03)),
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.Split, string.Empty, 0, 1, 0, new DateTime(2023, 01, 31)),

                    new("GOOGL34", "Google LLC", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 100, 2, 50, new DateTime(2023, 02, 01)), // Comprou 2 ativos por 50. Quantidade: 2 
                    new("TTWO", "Take Two Interactive", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 203, 2, 101.50, new DateTime(2023, 02, 01)), // Comprou outro BDR aleatório que não deve ser considerado 
                    new("GOOGL34", "Google LLC", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 240, 4, 60, new DateTime(2023, 02, 01)), // Comprou 4 ativos por 60. Quantidade: 6
                    new("GOOGL34", "Google LLC", B3ResponseConstants.BDRs, B3ResponseConstants.ReverseSplit, string.Empty, 0, 3, 0, new DateTime(2023, 02, 02)), // Grupamento de 1 pra 2. B3 vai retornar 3 porque é
                    // quantos ativos o investidor terá na posição após o grupamento.
                    // Preço médio: 340 / 3 = 113,33 look mom a dízima periódica

                    new("NVDC34", "NVIDIA CORP DRN", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 100, 2, 50, new DateTime(2023, 02, 01)), // Comprou 2 ativos por 50. Quantidade: 2 
                    new("NVDC34", "NVIDIA CORP DRN", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 240, 4, 60, new DateTime(2023, 02, 01)), // Comprou 4 ativos por 60. Quantidade: 6
                    new("NVDC34", "NVIDIA CORP DRN", B3ResponseConstants.BDRs, B3ResponseConstants.ReverseSplit, string.Empty, 0, 3, 0, new DateTime(2023, 02, 02)), // Grupamento de 1 pra 2. B3 vai retornar 3 porque é
                    // quantos ativos o investidor terá na posição após o grupamento. Quantidade: 3
                    new("NVDC34", "NVIDIA CORP DRN", B3ResponseConstants.BDRs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 180, 2, 180, new DateTime(2023, 02, 10)), // Comprou 2 ativos por 180. Quantidade: 5
                    // Preço médio: 520 / 5 = 104

                    // Mês 03
                    // Lucro day-trade: 23,79
                    // Lucro swing-trade: 35,56
                    // IR: 11,86 
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 190.92, 3, 63.64, new DateTime(2023, 03, 01)), // Comprou 3 ativos por 63.64. Quantidade: 3
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 87.43, 1, 87.43, new DateTime(2023, 03, 01), true), // Vendeu 1 ativo por 87.43. Quantidade: 2
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.BonusShare, string.Empty, 0, 10, 0, new DateTime(2023, 03, 02)), // Foi bonificado com 10 ativos. Quantidade: 12.

                    // B3 vai retornar 10 porque é a quantidade de ativos que será adicionado na posição do investidor após a bonificação.
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 50.43, 1, 50.43, new DateTime(2023, 03, 03)), // Comprou 1 ativo por 50.43. Quantidade: 13
                    new("BLMO11", "BVI OFFICE FUND II FII", B3ResponseConstants.FIIs, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 90, 2, 45, new DateTime(2023, 03, 04)), // Vendeu 2 ativos por 45. Quantidade: 11
                    // Preço médio: 263.92 / 11 = 23.99

                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 4954, 2, 2.477, new DateTime(2023, 05, 01)), // Comprou 2 ativos. Quantidade: 4
                    new("PETR4", "Petróleo Brasileiro S/A", B3ResponseConstants.Stocks, B3ResponseConstants.BonusShare, string.Empty, 0, 2, 0, new DateTime(2023, 05, 31)), // Foi bonificado com 2 ativos. Quantidade: 6
                    // Preço médio: 12707 / 6 = 2.117,83 
                }
            };
        }
    }
}
