using Microsoft.EntityFrameworkCore.ValueGeneration;
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
            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));
            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            bool paysIncomeTaxes = true;
            if (totalSoldInStocks >= IncomeTaxesConstants.LimitForStocksSelling) paysIncomeTaxes = true;

            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total = new();

            foreach (var movement in stocksMovements)
            {
                if (movement.MovementType.Equals(B3ServicesConstants.Buy))
                {
                    CalculateBuyOperations(total, movement);
                }
                if (movement.MovementType.Equals(B3ServicesConstants.Sell))
                {
                    CalculateSellOperations(total, movement, stocksMovements);
                }
                if (movement.MovementType.Equals(B3ServicesConstants.Split))
                {
                    CalculateSplitOperations(total, movement);
                }
                if (movement.MovementType.Equals(B3ServicesConstants.ReverseSplit))
                {
                    CalculateReverseSplit(total, movement);
                }
                if (movement.MovementType.Equals(B3ServicesConstants.BonusShare))
                {
                    CalculateBonusSharesOperations(total, movement, stocksMovements);
                }
            }

            if (paysIncomeTaxes)
            {
                response.TotalIncomeTaxesValue = total.Select(x => x.Value.IncomeTaxes).Sum();
            }

            response.Assets = DictionaryToList(total);
        }

        private static bool IsMovementDayTrade(Movement.EquitMovement movement, IEnumerable<Movement.EquitMovement> stocksMovements)
        {
            var referenceDateOperation = movement.ReferenceDate;

            var buys = stocksMovements.Where(x =>
                x.MovementType == B3ServicesConstants.Buy &&
                x.TickerSymbol == movement.TickerSymbol
            );

            if (buys.Select(x => x.ReferenceDate).Contains(referenceDateOperation))
                return true;
            else
                return false;
        }

        private void CalculateReverseSplit(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total, Movement.EquitMovement movement)
        {
            throw new NotImplementedException();
        }

        private static void CalculateBonusSharesOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total, Movement.EquitMovement movement, IEnumerable<Movement.EquitMovement> stocksMovements)
        {
            // É necessário calcular os agrupamentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TODO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de agrupamento.
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

                bool dayTradedTicker = IsMovementDayTrade(movement, stocksMovements);

                if (dayTradedTicker)
                {
                    asset.DayTraded = true;
                    asset.ValueToBeCompensated += +1;
                }
                else
                {
                    asset.ValueToBeCompensated += 0.005;
                }

                double profitPerShare = movement.UnitPrice - asset.AverageTradedPrice;

                asset.Profit += profitPerShare * movement.EquitiesQuantity;
                asset.IncomeTaxes = (double)CalculateStocksIncomeTaxes(asset);
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
                {
                    Ticker = asset.TickerSymbol,
                    TradeQuantity = (int)asset.Quantity,
                    TradeDateTime = asset.TradeDateTime,
                    TotalIncomeTaxesValue = asset.IncomeTaxes
                };
            }
        }

        private static decimal CalculateStocksIncomeTaxes(CalculateIncomeTaxesForTheFirstTime asset)
        {
            double totalTaxesPorcentage = IncomeTaxesConstants.IncomeTaxesForStocks - asset.ValueToBeCompensated;

            if (asset.DayTraded)
                totalTaxesPorcentage = IncomeTaxesConstants.IncomeTaxesForDayTrade - asset.ValueToBeCompensated;

            return ((decimal)totalTaxesPorcentage / 100m) * (decimal)asset.Profit;
        }
    }    
}
