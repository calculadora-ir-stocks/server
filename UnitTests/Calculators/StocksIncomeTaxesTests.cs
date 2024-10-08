﻿using Common.Enums;
using Core.Calculators;
using Core.Calculators.Assets;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace stocks_unit_tests.Business
{
    public class StocksIncomeTaxesTests
    {
        private readonly IIncomeTaxesCalculator stocksCalculator = new StocksIncomeTaxes();

        #region Day-trade unit tests

        [Theory(DisplayName = "Deve aplicar 20% de imposto em operações day-trade sob ações quando houver lucro.")]
        [MemberData(nameof(ProfitDayTradeData))]
        public void TestDayTradeProfit(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)stocks.DayTradeProfit;
            double expectedTotalSold = movements.Where(x => x.IsSell()).Select(x => x.OperationValue).Sum();

            Assert.Equal((double)expectedTaxes, stocks.Taxes);
            Assert.Equal(expectedTotalSold, stocks.TotalSold);
        }

        [Theory(DisplayName = "Não deve aplicar 20% de imposto em operações day-trade sob ações quando houver prejuízo.")]
        [MemberData(nameof(LossDayTradeData))]
        public void TestDayTradeLoss(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = 0;
            double expectedTotalSold = movements.Where(x => x.IsSell()).Select(x => x.OperationValue).Sum();

            Assert.Equal((double)expectedTaxes, stocks.Taxes);
            Assert.Equal(expectedTotalSold, stocks.TotalSold);
        }

        [Theory(DisplayName = "Não deve aplicar 20% de imposto em operações day-trade se a soma de todos os prejuízos e lucros forem negativos ou 0s.")]
        [MemberData(nameof(LossDayTradeDataWithProfit))] // Teve lucro, mas os prejuízos foram maiores; logo, não paga imposto.
        public void TestDayTradeLossWithProfit(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            double expectedTaxes = 0;
            double expectedTotalSold = movements.Where(x => x.IsSell()).Select(x => x.OperationValue).Sum();

            Assert.Equal(expectedTaxes, stocks.Taxes);
            Assert.Equal(expectedTotalSold, stocks.TotalSold);
        }

        public static IEnumerable<object[]> ProfitDayTradeData()
        {
            List<Movement.EquitMovement> movements = new();

            double buy = 10000;
            double sell = 15054.43;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 9432;
            sell = 11324;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.Sell, sell, 1, sell, new DateTime(2023, 01, 01), true));

            yield return new object[]
            {
                movements
            };
        }

        public static IEnumerable<object[]> LossDayTradeData()
        {
            List<Movement.EquitMovement> movements = new();

            int buy = 12324;
            double sell = 10324.32;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 5465;
            sell = 3487;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 2355;
            sell = 1324;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), true));

            yield return new object[]
            {
                movements
            };
        }

        public static IEnumerable<object[]> LossDayTradeDataWithProfit()
        {
            List<Movement.EquitMovement> movements = new();

            int buy = 12324;
            double sell = 10324.32;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 5465;
            sell = 3487;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), true));

            buy = 2355;
            sell = 3324;

            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), true));
            movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), true));

            yield return new object[]
            {
                movements
            };
        }

        #endregion

        #region Swing-trade unit tests

        [Theory(DisplayName = "Deve aplicar 15% de impostos em operações swing-trade sob ações quando > 20k forem vendidos.")]
        [MemberData(nameof(ProfitSwingTradeDataMoreThan20k))]
        public void TestSwingTradeProfitMoreThan20kSold(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocks.SwingTradeProfit;
            double expectedTotalSold = movements.Where(x => x.IsSell()).Select(x => x.OperationValue).Sum();

            Assert.Equal((double)expectedTaxes, stocks.Taxes);
            Assert.Equal(expectedTotalSold, stocks.TotalSold);
        }

        [Theory(DisplayName = "Não deve aplicar 15% de impostos em operações swing-trade sob ações quando < 20k forem vendidos.")]
        [MemberData(nameof(ProfitSwingTradeDataLessThan20k))]
        public void TestSwingTradeProfitLessThan20kSold(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocksResponse = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal expectedTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocksResponse.SwingTradeProfit;
            double expectedTotalSold = movements.Where(x => x.IsSell()).Select(x => x.OperationValue).Sum();

            Assert.Equal(0, stocksResponse.Taxes);
            Assert.Equal(expectedTotalSold, stocksResponse.TotalSold);
        }

        public static IEnumerable<object[]> ProfitSwingTradeDataMoreThan20k()
        {
            List<Movement.EquitMovement> movements = new()
            {
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 24043, 1, 24043, new DateTime(2023, 01, 01), false),
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 28394, 1, 28394, new DateTime(2023, 01, 01), false),
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 9032, 1, 9032, new DateTime(2023, 01, 01), false),
                new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 6500, 1, 6500, new DateTime(2023, 01, 01), false)
            };

            // Lucro em operações com vendas em > 20k.
            yield return new object[]
            {
                movements
            };
        }

        public static IEnumerable<object[]> ProfitSwingTradeDataLessThan20k()
        {
            List<Movement.EquitMovement> movements = new();
            int movementsQuantity = Random.Shared.Next(0, 100);

            for (int i = 0; i <= movementsQuantity; i++)
            {
                // lucro
                int buy = Random.Shared.Next(0, 10);
                int sell = Random.Shared.Next(0, 100);

                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, buy, 1, buy, new DateTime(2023, 01, 01), false));
                movements.Add(new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, sell, 1, sell, new DateTime(2023, 01, 01), false));
            }

            // Lucro em operações com vendas em < 20k.
            yield return new object[]
            {
                movements
            };
        }

        #endregion

        #region Day-trade and swing-trade unit tests

        [Theory(DisplayName = "Deve aplicar 20% de impostos sobre operações day-trade, mas 0% em operações swing-trade caso < 20k tenha sido vendido.")]
        [MemberData(nameof(DayTradeAndSwingTradeProfitLessThan20k))]
        public void TestBothDayTradeAndSwingTradeLessThan20k(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal swingTradeTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocks.SwingTradeProfit;
            decimal dayTradeTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)stocks.DayTradeProfit;

            double totalSold = movements.Where(x => x.IsSell()).Select(x => x.OperationValue).Sum();
            double expectedTaxes = 200;

            Assert.Equal(totalSold, stocks.TotalSold);
            Assert.Equal(expectedTaxes, stocks.Taxes);
        }

        [Theory(DisplayName = "Deve aplicar 20% de impostos sobre operações day-trade e 15% em operações swing-trade caso > 20k tenha sido vendido.")]
        [MemberData(nameof(DayTradeAndSwingTradeProfitMoreThan20k))]
        public void TestBothDayTradeAndSwingTradeMoreThan20k(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "01");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();

            decimal swingTradeTaxes = (AliquotConstants.IncomeTaxesForStocks / 100m) * (decimal)stocks.SwingTradeProfit;
            decimal dayTradeTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)stocks.DayTradeProfit;

            double totalSold = movements.Where(x => x.IsSell()).Select(x => x.OperationValue).Sum();
            double expectedTaxes = 2568.2;

            Assert.Equal(totalSold, stocks.TotalSold);
            Assert.Equal(expectedTaxes, stocks.Taxes);
        }

        [Theory(DisplayName = "Deve calcular corretamente o prejuízo (caso de debug).")]
        [MemberData(nameof(LossMovements))]
        public void ShouldCalculateLossCorrectly(List<Movement.EquitMovement> movements)
        {
            InvestorMovementDetails response = new();

            stocksCalculator.Execute(response, movements, "08");

            AssetIncomeTaxes stocks = response.Assets.Where(x => x.AssetTypeId == Asset.Stocks).Single();
            Assert.Equal(-6.28, Convert.ToDouble(stocks.SwingTradeProfit.ToString("00.00")));
        }

        public static IEnumerable<object[]> DayTradeAndSwingTradeProfitLessThan20k()
        {
            // Lucro em operações de swing-trade e day-trade, mas < 20k foram vendidos. Nesse caso, apenas 20% devem ser aplicados nas operações
            // de day-trade.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 5467, 1, 5467, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 6587, 1, 6587, new DateTime(2023, 01, 02)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 7653, 1, 7653, new DateTime(2023, 01, 03), true),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 8653, 1, 8653, new DateTime(2023, 01, 03), true)
                }
            };
        }

        public static IEnumerable<object[]> DayTradeAndSwingTradeProfitMoreThan20k()
        {
            // Lucro em operações de swing-trade e day-trade, mas > 20k foram vendidos. Nesse caso, 20% devem ser aplicados nas operações
            // de day-trade e 15% devem ser aplicados nas operações de swing-trade. O imposto total a ser pago é a soma dessas duas aplicações.
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 5467, 1, 5467, new DateTime(2023, 01, 01)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 6587, 1, 6587, new DateTime(2023, 01, 02)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 7653, 1, 7653, new DateTime(2023, 01, 03), true),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 19654, 1, 19654, new DateTime(2023, 01, 03), true)
                }
            };
        }

        public static IEnumerable<object[]> LossMovements()
        {
            yield return new object[]
            {
                new List<Movement.EquitMovement>
                {
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 58.31, 7, 8.33, new DateTime(2023, 08, 03)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 8.4, 1, 8.4, new DateTime(2023, 08, 04)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 84.2, 10, 8.42, new DateTime(2023, 08, 07)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.BuyOperationType, 93.17, 11, 8.47, new DateTime(2023, 08, 09)),
                    new("PETR4", "Petróleo Brasileiro S/A", "Ações", B3ResponseConstants.TransferenciaLiquidacao, B3ResponseConstants.SellOperationType, 237.8, 29, 8.2, new DateTime(2023, 08, 23)),
                }
            };
        }

        #endregion
    }
}
