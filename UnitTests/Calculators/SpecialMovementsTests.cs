using Core.Calculators;
using Core.Constants;
using Core.Models;
using Core.Models.B3;
using Microsoft.AspNetCore.Authentication;

namespace stocks_unit_tests.Calculators
{
    /// <summary>
    /// Testes relacionados a Desdobramento, Grupamento e Bonificações de ativos.
    /// </summary>
    public class SpecialMovementsTests : ProfitCalculator
    {
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
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.Split, 0, split, 0, new DateTime(2023, 01, 01), false),
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
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.ReverseSplit, 0, newQuantityInPortfolio, 0, new DateTime(2023, 01, 01), false),
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
        [Theory(DisplayName = "Deve calcular corretamente uma operação de grupamento ao dividir o total comprado " +
            "com a nova quantidade de ativos na posição atual. Depois, deve ser feito o cálculo do preço médio com base na nova alteração.")]
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
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.BonusShare, bonusShareOperationValue, bonusShareQuantity, 0, new DateTime(2023, 01, 01), false),
            };

            CalculateProfitAndAverageTradedPrice(movement, averageTradedPrices);

            var expectedPrice = (totalBought + bonusShareOperationValue) / (bonusShareQuantity + quantity);

            Assert.Equal(expectedPrice, averageTradedPrices.First().AverageTradedPrice);
        }
        #endregion
    }
}
