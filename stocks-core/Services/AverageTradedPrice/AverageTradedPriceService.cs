using stocks_common.DTOs.AverageTradedPrice;
using stocks_core.Constants;
using stocks_core.DTOs.B3;

namespace stocks_core.Services.AverageTradedPrice
{
    // TODO: change to static class
    public class AverageTradedPriceService : IAverageTradedPriceService
    {
        public Dictionary<string, AverageTradedPriceCalculatorResponse> CalculateAverageTradedPrice(
            IEnumerable<Movement.EquitMovement> buyOperations,
            IEnumerable<Movement.EquitMovement> sellOperations,
            IEnumerable<Movement.EquitMovement> splitsOperations,
            IEnumerable<Movement.EquitMovement> bonusSharesOperations
        )
        {
            Dictionary<string, AverageTradedPriceCalculatorResponse> total = new();

            IEnumerable<Movement.EquitMovement> allMovements = buyOperations.Concat(sellOperations).Concat(splitsOperations).Concat(bonusSharesOperations);
            allMovements = allMovements.OrderBy(x => x.ReferenceDate).ToList();

            foreach (var movement in allMovements)
            {
                if (movement.MovementType.Equals(B3ServicesConstants.Buy))
                {
                    if (total.ContainsKey(movement.TickerSymbol))
                    {
                        var asset = total[movement.TickerSymbol];

                        // TODO: calcular taxas de corretagem + taxas da fonte
                        asset.CurrentPrice += movement.OperationValue;
                        asset.CurrentQuantity += movement.EquitiesQuantity;
                    }
                    else
                    {
                        total.Add(movement.TickerSymbol, new AverageTradedPriceCalculatorResponse(
                            movement.OperationValue,
                            movement.EquitiesQuantity,
                            movement.TickerSymbol,
                            movement.ReferenceDate.ToString("yyyy-MM")
                        ));
                    }
                }
                if (movement.MovementType.Equals(B3ServicesConstants.Sell))
                {
                    if (total.ContainsKey(movement.TickerSymbol))
                    {
                        var asset = total[movement.TickerSymbol];
                    }
                    else
                    {
                        // Primeira vez que um ticker aparece é em uma operação de venda e o usuário não especificou ela no body do método.
                        // Se sim, o ticker foi comprado antes de 01/11/2019. Se não foi especificado pelo usuário, pode apenas retornar
                        // uma exceção para o client que o preço médio desse ativo é necessário.
                    }
                }
                // TODO: calcular os desdobramentos

                // É necessário verificar quando houve desdobramentos, pois caso não faça,
                // a quantidade de papéis de um ativo pode aumentar e a relação de preço/quantidade
                // estará incorreta.
                if (movement.MovementType.Equals(B3ServicesConstants.Split))
                {

                }
                // TODO: calcular bonificações
                if (movement.MovementType.Equals(B3ServicesConstants.BonusShare))
                {

                }
            }

            foreach (var movement in total)
            {
                var totalSpent = movement.Value.CurrentPrice;
                var assetQuantity = movement.Value.CurrentQuantity;

                var averageTradedPrice = totalSpent / assetQuantity;

                movement.Value.AverageTradedPrice = FormatToTwoDecimalPlaces(averageTradedPrice);
            }

            return total;
        }

        private static double FormatToTwoDecimalPlaces(double value)
        {
            return Math.Truncate((100 * (value))) / 100;
        }
    }
}
