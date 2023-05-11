using Microsoft.IdentityModel.Tokens;
using stocks_common.Models;
using stocks_core.Constants;
using stocks_core.DTOs.B3;

namespace stocks_core.Business
{
    /// <summary>
    /// Classe responsável por calcular o preço médio em movimentações de compra e venda.
    /// Também leva em consideração movimentos de bonificação, desdobramento e agrupamento.
    /// </summary>
    public class AverageTradedPriceCalculator
    {
        private static readonly List<TickerAverageTradedPrice> tickersAverageTradedPrice = new();

        /// <summary>
        /// Calcula o preço médio dos ativos que foram operados e calcula o imposto de renda a ser pago
        /// nas operações especificadas.
        /// </summary>
        public static Dictionary<string, TickerDetails> CalculateMovements
            (IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, TickerDetails> response = new();

            foreach (var movement in movements)
            {
                switch (movement.MovementType)
                {
                    case B3ServicesConstants.Buy:
                        CalculateBuyOperations(response, movement, movements);
                        break;
                    case B3ServicesConstants.Sell:
                        CalculateSellOperations(response, movement, movements);
                        break;
                    case B3ServicesConstants.Split:
                        CalculateSplitOperations(response, movement);
                        break;
                    case B3ServicesConstants.ReverseSplit:
                        CalculateReverseSplit(response, movement);
                        break;
                    case B3ServicesConstants.BonusShare:
                        CalculateBonusSharesOperations(response, movement, movements);
                        break;
                }
            }

            return response;
        }

        public static decimal CalculateIncomeTaxes(double swingTradeProfit, double dayTradeProfit, int aliquot)
        {
            decimal swingTradeTaxes = 0;
            decimal dayTradeTaxes = 0;

            if (swingTradeProfit > 0)
            {
                swingTradeTaxes = (aliquot / 100m) * (decimal)swingTradeProfit;
            }

            if (dayTradeProfit > 0)
            {
                dayTradeTaxes = (IncomeTaxesConstants.IncomeTaxesForDayTrade / 100m) * (decimal)dayTradeProfit;
            }

            decimal totalTaxes = swingTradeTaxes + dayTradeTaxes;

            return totalTaxes;
        }

        public static IEnumerable<(string, string)> DictionaryToList(Dictionary<string, TickerDetails> total)
        {
            foreach (var asset in total.Values)
            {
                yield return (asset.TickerSymbol, asset.CorporationName);
            }
        }
    
        public static bool TickerDayTraded(IEnumerable<Movement.EquitMovement> movements, string tickerSymbol)
        {
            var buys = movements.Where(x =>
                x.MovementType == B3ServicesConstants.Buy && x.TickerSymbol == tickerSymbol
            );
            var sells = movements.Where(x =>
                x.MovementType == B3ServicesConstants.Sell && x.TickerSymbol == tickerSymbol
            );

            var dayTradeTransactions = buys.Where(b => sells.Any(s =>
                s.ReferenceDate == b.ReferenceDate &&
                s.TickerSymbol == b.TickerSymbol
            ));

            return dayTradeTransactions.Any();
        }

        public static List<TickerAverageTradedPrice> GetListContainingAverageTradedPrices()
        {
            return tickersAverageTradedPrice;
        }

        private static void CalculateBuyOperations(Dictionary<string, TickerDetails> response, Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> movements)
        {
            if (response.ContainsKey(movement.TickerSymbol))
            {
                var asset = response[movement.TickerSymbol];

                asset.Price += movement.OperationValue;
                asset.Quantity += movement.EquitiesQuantity;
                asset.MonthAverageTradedPrice = asset.Price / asset.Quantity;
                // TO-DO (MVP?): calcular emolumentos.
            }
            else
            {
                double averageTradedPrice = movement.OperationValue / movement.EquitiesQuantity;

                var ticker = new TickerDetails(
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.TickerSymbol,
                    movement.CorporationName,
                    averageTradedPrice,
                    dayTraded: TickerDayTraded(movements, movement.TickerSymbol)
                );

                response.Add(movement.TickerSymbol, ticker);
            }

            UpdateTickerAverageTradedPrice(
                tickerSymbol: movement.TickerSymbol,
                operationValue: movement.OperationValue,
                tradedQuantity: (int)movement.EquitiesQuantity
            );
        }        

        private static void CalculateSellOperations(Dictionary<string, TickerDetails> response, Movement.EquitMovement movement, IEnumerable<Movement.EquitMovement> movements)
        {
            if (response.ContainsKey(movement.TickerSymbol))
            {
                var asset = response[movement.TickerSymbol];

                double profitPerShare = movement.UnitPrice - asset.MonthAverageTradedPrice;
                double totalProfit = profitPerShare * movement.EquitiesQuantity;

                if (totalProfit > 0)
                {
                    // TO-DO (MVP?): calcular IRRFs (e.g dedo-duro).
                }

                asset.Profit += totalProfit;

                // TO-DO (MVP?): calcular emolumentos.
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.
                response.Add(movement.TickerSymbol, new TickerDetails(
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.TickerSymbol,
                    movement.CorporationName,
                    monthAverageTradedPrice: 0,
                    dayTraded: TickerDayTraded(movements, movement.TickerSymbol),
                    tickerBoughtBeforeB3DateRange: true
                ));

                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        private static void CalculateSplitOperations(Dictionary<string, TickerDetails> response, Movement.EquitMovement movement)
        {
            // É necessário calcular os desdobramentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de desdobramento.
            throw new NotImplementedException();
        }

        private static void CalculateReverseSplit(Dictionary<string, TickerDetails> response, Movement.EquitMovement movement)
        {
            // É necessário calcular os agrupamentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de agrupamento.
            throw new NotImplementedException();
        }

        private static void CalculateBonusSharesOperations(Dictionary<string, TickerDetails> response, Movement.EquitMovement movement, IEnumerable<Movement.EquitMovement> movements)
        {
            // É necessário calcular as bonificações de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de bonificação.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Atualiza o preço médio de um determinado ativo.
        /// </summary>
        private static void UpdateTickerAverageTradedPrice(string tickerSymbol, double operationValue, int tradedQuantity)
        {
            var ticker = tickersAverageTradedPrice.Where(x => x.TickerSymbol == tickerSymbol).FirstOrDefault();

            if (ticker is null)
            {
                double averageTradedPrice = operationValue / tradedQuantity;
                tickersAverageTradedPrice.Add(new TickerAverageTradedPrice(tickerSymbol, averageTradedPrice, operationValue, tradedQuantity));
            }
            else
            {
                ticker.TotalBought += operationValue;
                ticker.TradedQuantity += tradedQuantity;

                ticker.AverageTradedPrice = ticker.TotalBought / ticker.TradedQuantity;
            }
        }
    }    
}
