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
        /// <summary>
        /// Calcula o preço médio de um determinado ativo.
        /// </summary>
        public static Dictionary<string, CalculateIncomeTaxesForTheFirstTime> CalculateAverageTradedPrice(IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, CalculateIncomeTaxesForTheFirstTime> response = new();

            foreach (var movement in movements)
            {
                switch (movement.MovementType)
                {
                    case B3ServicesConstants.Buy:
                        CalculateBuyOperations(response, movement);
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

        private static void CalculateBuyOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> response, Movement.EquitMovement movement)
        {
            if (!response.ContainsKey(movement.TickerSymbol))
            {
                double averageTradedPrice = movement.OperationValue / movement.EquitiesQuantity;

                var ticker = new CalculateIncomeTaxesForTheFirstTime(
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.TickerSymbol,
                    movement.CorporationName,
                    movement.ReferenceDate.ToString("MM-yyyy"),
                    averageTradedPrice
                );

                response.Add(movement.TickerSymbol, ticker);
            }
            else
            {
                var asset = response[movement.TickerSymbol];

                asset.Price += movement.OperationValue;
                asset.Quantity += movement.EquitiesQuantity;

                asset.AverageTradedPrice = asset.Price / asset.Quantity;

                // TO-DO (MVP?): calcular emolumentos.
            }
        }

        private static void CalculateSellOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> response, Movement.EquitMovement movement, IEnumerable<Movement.EquitMovement> movements)
        {
            if (response.ContainsKey(movement.TickerSymbol))
            {
                var asset = response[movement.TickerSymbol];

                double profitPerShare = movement.UnitPrice - asset.AverageTradedPrice;
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
                response.Add(movement.TickerSymbol, new CalculateIncomeTaxesForTheFirstTime(
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.TickerSymbol,
                    movement.CorporationName,
                    movement.ReferenceDate.ToString("MM-yyyy"),
                    averageTradedPrice: 0,
                    tickerBoughtBeforeB3DateRange: true
                ));

                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        private static void CalculateSplitOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> response, Movement.EquitMovement movement)
        {
            // É necessário calcular os desdobramentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de desdobramento.
            throw new NotImplementedException();
        }

        private static void CalculateReverseSplit(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> response, Movement.EquitMovement movement)
        {
            // É necessário calcular os agrupamentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de agrupamento.
            throw new NotImplementedException();
        }

        private static void CalculateBonusSharesOperations(Dictionary<string, CalculateIncomeTaxesForTheFirstTime> response, Movement.EquitMovement movement, IEnumerable<Movement.EquitMovement> movements)
        {
            // É necessário calcular as bonificações de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de bonificação.
            throw new NotImplementedException();
        }
    }
}
