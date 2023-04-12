﻿using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.IdentityModel.Tokens;
using stocks_common.Models;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Business
{
    public class StocksIncomeTaxes : IIncomeTaxesCalculator
    {
        private readonly IAverageTradedPriceRepostory _averageTradedPriceRepository;
        private readonly IAverageTradedPriceService _averageTradedPriceService;

        public StocksIncomeTaxes(IAverageTradedPriceRepostory averageTradedPriceRepository, IAverageTradedPriceService averageTradedPriceService)
        {
            _averageTradedPriceRepository = averageTradedPriceRepository;
            _averageTradedPriceService = averageTradedPriceService;
        }

        public async Task CalculateCurrentMonthIncomeTaxes(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            if (totalSoldInStocks > IncomeTaxesConstants.LimitForStocksSelling) return;

            foreach(var movement in stocksMovements)
            {
                // TODO: calculate day-trade.
                // if user day-traded this ticker, pays 20%

                var stockBuys = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Buy)
                );

                var stockSells = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Sell)
                );

                var stockSplits = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.Split)
                );

                var stockBonusShares = stocksMovements.Where(x =>
                    x.TickerSymbol.Equals(movement.TickerSymbol) &&
                    x.MovementType.Equals(B3ServicesConstants.BonusShare)
                );

                var averageTradedPrice = _averageTradedPriceService.CalculateAverageTradedPrice(stockBuys, stockSells, stockSplits, stockBonusShares);
                var currentTickerAverageTradedPrice = _averageTradedPriceRepository.GetAverageTradedPrice(movement.TickerSymbol, accountId)!;

                double profit = averageTradedPrice[movement.TickerSymbol].AverageTradedPrice - currentTickerAverageTradedPrice.AveragePrice;
            }
        }

        public void CalculateIncomeTaxesForAllMonths(CalculateAssetsIncomeTaxesResponse response, IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {            
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total = new();

            foreach (var movement in stocksMovements)
            {
                switch (movement.MovementType)
                {
                    case B3ServicesConstants.Buy:
                        CalculateBuyOperations(total, movement);
                        break;
                    case B3ServicesConstants.Sell:
                        CalculateSellOperations(total, movement, stocksMovements);
                        break;
                    case B3ServicesConstants.Split:
                        CalculateSplitOperations(total, movement);
                        break;
                    case B3ServicesConstants.ReverseSplit:
                        CalculateReverseSplit(total, movement);
                        break;
                    case B3ServicesConstants.BonusShare:
                        CalculateBonusSharesOperations(total, movement, stocksMovements);
                        break;
                }
            }

            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));
            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            bool sellsSuperiorThan20000 = totalSoldInStocks >= IncomeTaxesConstants.LimitForStocksSelling;

            double totalProfit = total.Select(x => x.Value.Profit).Sum();
            bool dayTraded = InvestorDayTraded(stocksMovements);

            bool paysIncomeTaxes = (sellsSuperiorThan20000 && totalProfit > 0) || (dayTraded && totalProfit > 0);

            if (paysIncomeTaxes)
            {
                response.TotalIncomeTaxesValue = (double)CalculateStocksIncomeTaxes(totalProfit, dayTraded);
            }

            double totalLoss = total.Select(x => x.Value.Profit).Where(x => x < 0).Sum();
            response.CompensateLoss = totalLoss;

            response.Assets = DictionaryToList(total);
        }

        private static bool InvestorDayTraded(IEnumerable<Movement.EquitMovement> stocksMovements)
        {
            var buys = stocksMovements.Where(x =>
                x.MovementType == B3ServicesConstants.Buy
            );
            var sells = stocksMovements.Where(x =>
                x.MovementType == B3ServicesConstants.Sell
            );

            var dayTradeTransactions = buys.Where(b => sells.Any(s => 
                s.ReferenceDate == b.ReferenceDate && 
                s.TickerSymbol == b.TickerSymbol
            ));

            return dayTradeTransactions.Any();
        }

        private static void CalculateReverseSplit(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total, Movement.EquitMovement movement)
        {
            // É necessário calcular os agrupamentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TODO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de agrupamento.
            throw new NotImplementedException();
        }

        private static void CalculateBonusSharesOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total, Movement.EquitMovement movement, IEnumerable<Movement.EquitMovement> stocksMovements)
        {
            // É necessário calcular as bonificações de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TODO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de bonificação.
            throw new NotImplementedException();
        }

        private static void CalculateSplitOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total, Movement.EquitMovement movement)
        {
            // É necessário calcular os desdobramentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TODO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de desdobramento.
            throw new NotImplementedException();
        }

        private static void CalculateSellOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total, Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> stocksMovements)
        {
            if (total.ContainsKey(movement.TickerSymbol))
            {
                var asset = total[movement.TickerSymbol];

                double profitPerShare = movement.UnitPrice - asset.AverageTradedPrice;
                double totalProfit = profitPerShare * movement.EquitiesQuantity;

                if (totalProfit > 0)
                {
                    // TODO: calcular IRRFs (e.g dedo-duro).
                }

                asset.Profit += totalProfit;
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.
                total.Add(movement.TickerSymbol, new CalculateIncomeTaxesForTheFirstTime(
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.TickerSymbol,
                    movement.ReferenceDate.ToString("MM-yyyy"),
                    averageTradedPrice: 0,
                    tickerBoughtBeforeB3DateRange: true
                ));

                stocksMovements = stocksMovements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        private static void CalculateBuyOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total, Movement.EquitMovement movement)
        {
            if (!total.ContainsKey(movement.TickerSymbol))
            {
                double averageTradedPrice = movement.OperationValue / movement.EquitiesQuantity;

                var ticker = new CalculateIncomeTaxesForTheFirstTime(
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.TickerSymbol,
                    movement.ReferenceDate.ToString("MM-yyyy"),
                    averageTradedPrice
                );

                total.Add(movement.TickerSymbol, ticker);
            }
            else
            {
                var asset = total[movement.TickerSymbol];

                asset.Price += movement.OperationValue;
                asset.Quantity += movement.EquitiesQuantity;

                asset.AverageTradedPrice = asset.Price / asset.Quantity;
            }
        }

        private static IEnumerable<Asset> DictionaryToList(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total)
        {
            foreach(var asset in total.Values)
            {
                yield return new Asset
                (
                    asset.TickerSymbol,
                    asset.AverageTradedPrice
                );
            }
        }

        private static decimal CalculateStocksIncomeTaxes(double value, bool dayTraded)
        {
            double totalTaxesPorcentage = IncomeTaxesConstants.IncomeTaxesForStocks;

            if (dayTraded)
                totalTaxesPorcentage = IncomeTaxesConstants.IncomeTaxesForDayTrade;

            return ((decimal)totalTaxesPorcentage / 100m) * (decimal)value;
        }
    }    
}
