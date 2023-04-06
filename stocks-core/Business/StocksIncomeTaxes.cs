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

        public async Task AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
        {
            var sells = stocksMovements.Where(x => x.MovementType.Equals(B3ServicesConstants.Sell));

            double totalSoldInStocks = sells.Sum(stock => stock.OperationValue);

            // TO-DO: change operator
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

        public void CalculateIncomeTaxesForTheFirstTimeAndSaveAverageTradedPrice(CalculateAssetsIncomeTaxesResponse response, IEnumerable<Movement.EquitMovement> stocksMovements, Guid accountId)
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
                    if (!total.ContainsKey(movement.TickerSymbol))
                    {
                        var ticker = new CalculateIncomeTaxesForTheFirstTime(
                            movement.OperationValue,
                            movement.EquitiesQuantity,
                            movement.TickerSymbol,
                            movement.ReferenceDate.ToString("MM-yyyy")
                        );
                        ticker.AverageTradedPrice = movement.OperationValue / movement.EquitiesQuantity;

                        total.Add(movement.TickerSymbol, ticker);
                    } else
                    {
                        var asset = total[movement.TickerSymbol];

                        // TODO: calcular taxas de corretagem + taxas da fonte
                        asset.CurrentPrice += movement.OperationValue;
                        asset.CurrentQuantity += movement.EquitiesQuantity;

                        asset.AverageTradedPrice = asset.CurrentPrice / asset.CurrentQuantity;
                    }
                }

                if (movement.MovementType.Equals(B3ServicesConstants.Sell))
                {
                    if (total.ContainsKey(movement.TickerSymbol))
                    {
                        var asset = total[movement.TickerSymbol];

                        double profitPerShare = movement.UnitPrice - asset.AverageTradedPrice;

                        asset.Profit += profitPerShare * movement.EquitiesQuantity;
                        asset.IncomeTaxes = (double)CalculateStocksIncomeTaxes(asset.Profit);
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
                            true
                        ));
                        stocksMovements = stocksMovements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
                    }
                }
            }

            if (paysIncomeTaxes)
            {
                double totalProfit = total.Select(x => x.Value.Profit).Sum();
                response.TotalIncomeTaxesValue = (double)CalculateStocksIncomeTaxes(totalProfit);
            }

            response.Assets = DictionaryToList(total);
        }

        private static IEnumerable<Asset> DictionaryToList(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> total)
        {
            foreach(var asset in total.Values)
            {
                yield return new Asset
                {
                    Ticker = asset.TickerSymbol,
                    TradeQuantity = (int)asset.CurrentQuantity,
                    TradeDateTime = asset.TradeDateTime,
                    TotalIncomeTaxesValue = asset.IncomeTaxes
                };
            }
        }

        private static decimal CalculateStocksIncomeTaxes(double value)
        {
            return (IncomeTaxesConstants.IncomeTaxesForStocks / 100m) * (decimal)value;
        }
    }    
}
