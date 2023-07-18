using Microsoft.IdentityModel.Tokens;
using stocks_common.Helpers;
using stocks_common.Models;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using Asset = stocks_common.Enums.Asset;

namespace stocks_core.Calculators
{
    /// <summary>
    /// Classe responsável por calcular o preço médio e o lucro de movimentações de compra e venda.
    /// Também leva em consideração movimentos de bonificação, desdobramento e agrupamento.
    /// </summary>
    public abstract class AverageTradedPriceCalculator
    {
        /// <summary>
        /// Calcula o lucro das movimentações especificadas levando em consideração o preço médio dos ativos.
        /// </summary>
        public static (List<OperationDetails> dayTrade, List<OperationDetails> swingTrade) CalculateProfit
            (IEnumerable<Movement.EquitMovement> movements, List<AverageTradedPriceDetails> movementsAverageTradedPrices)
        {
            List<OperationDetails> dayTrade = new();
            List<OperationDetails> swingTrade = new();

            foreach (var movement in movements)
            {
                switch (movement.MovementType)
                {
                    case B3ResponseConstants.Buy:
                        UpdateAverageTradedPrice(movement, movementsAverageTradedPrices!);
                        break;
                    case B3ResponseConstants.Sell:
                        UpdateProfitOrLoss(dayTrade, swingTrade, movement, movements, movementsAverageTradedPrices);
                        break;
                    case B3ResponseConstants.Split:
                        CalculateSplitOperation(movement);
                        break;
                    case B3ResponseConstants.ReverseSplit:
                        CalculateReverseSplitOperation(movement);
                        break;
                    case B3ResponseConstants.BonusShare:
                        CalculateBonusSharesOperation(movement);
                        break;
                }
            }

            return (dayTrade, swingTrade);
        }

        public static decimal CalculateIncomeTaxes(double swingTradeProfit, double dayTradeProfit, int aliquot)
        {
            decimal swingTradeTaxes = 0;
            decimal dayTradeTaxes = 0;

            if (swingTradeProfit > 0)
                swingTradeTaxes = (aliquot / 100m) * (decimal)swingTradeProfit;

            if (dayTradeProfit > 0)
                dayTradeTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)dayTradeProfit;

            decimal totalTaxes = swingTradeTaxes + dayTradeTaxes;

            return totalTaxes;
        }

        /// <summary>
        /// Retorna as operações day-trade e swing-trade concatenadas.
        /// </summary>
        public static IEnumerable<OperationDetails> ConcatOperations(
            List<OperationDetails>? dayTradeMovements,
            List<OperationDetails>? swingTradeMovements
        )
        {
            if (!dayTradeMovements.IsNullOrEmpty() && !swingTradeMovements.IsNullOrEmpty())
            {
                return dayTradeMovements!.Concat(swingTradeMovements!);
            }

            if (!dayTradeMovements.IsNullOrEmpty()) return dayTradeMovements;

            if (!swingTradeMovements.IsNullOrEmpty()) return swingTradeMovements;

            return Array.Empty<OperationDetails>();
        }

        private static void AddTickerIntoResponseDictionary(
            List<OperationDetails> dayTradeResponse,
            List<OperationDetails> swingTradeResponse,
            Movement.EquitMovement movement
        )
        {
            if (TickerAlreadyAdded(dayTradeResponse, swingTradeResponse, movement)) return;

            if (movement.DayTraded)
            {
                dayTradeResponse.Add(new OperationDetails(
                    movement.ReferenceDate.Day.ToString(),
                    movement.TickerSymbol,
                    movement.CorporationName
                ));
            }
            else
            {
                swingTradeResponse.Add(new OperationDetails(
                    movement.ReferenceDate.Day.ToString(),
                    movement.TickerSymbol,
                    movement.CorporationName
                ));
            }
        }

        private static bool TickerAlreadyAdded(List<OperationDetails> dayTradeResponse, List<OperationDetails> swingTradeResponse, Movement.EquitMovement movement)
        {
            return dayTradeResponse.Select(x => x.TickerSymbol).Equals(movement.TickerSymbol) ||
                swingTradeResponse.Select(x => x.TickerSymbol).Equals(movement.TickerSymbol);
        }

        private static void UpdateAverageTradedPrice(
            Movement.EquitMovement movement,
            List<AverageTradedPriceDetails> averageTradedPrices,
            bool sellOperation = false
        )
        {
            bool tickerHasAverageTradedPrice = averageTradedPrices.Select(x => x.TickerSymbol).Contains(movement.TickerSymbol);

            if (tickerHasAverageTradedPrice)
            {
                var ticker = averageTradedPrices.Where(x => x.TickerSymbol == movement.TickerSymbol).First();

                double totalBought;
                double quantity;

                if (sellOperation)
                {
                    totalBought = ticker.TotalBought - movement.OperationValue;
                    quantity = ticker.TradedQuantity - movement.EquitiesQuantity;
                }
                else
                {
                    totalBought = ticker.TotalBought + movement.OperationValue;
                    quantity = ticker.TradedQuantity + movement.EquitiesQuantity;
                }

                ticker.UpdateValues(totalBought, (int)quantity);

                if (ticker.SoldOut) averageTradedPrices.Remove(ticker);
            }
            else
            {
                averageTradedPrices.Add(new AverageTradedPriceDetails(
                    movement.TickerSymbol,
                    averageTradedPrice: movement.OperationValue / movement.EquitiesQuantity,
                    totalBought: movement.OperationValue,
                    tradedQuantity: (int)movement.EquitiesQuantity,
                    AssetTypeHelper.GetEnumByName(movement.AssetType)
                ));
            }
        }

        private static void UpdateProfitOrLoss(
            List<OperationDetails> dayTradeResponse,
            List<OperationDetails> swingTradeResponse,
            Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> movements,
            List<AverageTradedPriceDetails> averageTradedPrices
        )
        {
            AddTickerIntoResponseDictionary(dayTradeResponse, swingTradeResponse, movement);

            OperationDetails? asset = null;

            if (movement.DayTraded)
                asset = dayTradeResponse.First(x => x.TickerSymbol == movement.TickerSymbol);
            else
                asset = swingTradeResponse.First(x => x.TickerSymbol == movement.TickerSymbol);

            if (AssetBoughtAfterB3MinimumDate(movement, averageTradedPrices))
            {
                var averageTradedPrice = averageTradedPrices.Where(x => x.TickerSymbol == movement.TickerSymbol).First();

                double profitPerShare = movement.UnitPrice - averageTradedPrice.AverageTradedPrice;
                double totalProfit = profitPerShare * movement.EquitiesQuantity;

                if (totalProfit > 0)
                {
                    // TO-DO (MVP?): calcular IRRFs (e.g dedo-duro).
                    // TO-DO (MVP?): calcular emolumentos.
                }

                asset.UpdateProfit(totalProfit, movement.ReferenceDate.Day.ToString());
                asset.UpdateTotalSold(movement.OperationValue);

                UpdateAverageTradedPrice(movement, averageTradedPrices, sellOperation: true);
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.

                asset.UpdateTickerBoughtBeforeB3DateRange(boughtBeforeB3DateRange: true);
                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        private static bool AssetBoughtAfterB3MinimumDate(Movement.EquitMovement movement, List<AverageTradedPriceDetails> averageTradedPrices)
        {
            return averageTradedPrices.Where(x => x.TickerSymbol == movement.TickerSymbol).FirstOrDefault() != null;
        }

        private static void CalculateSplitOperation(Movement.EquitMovement movement)
        {
            // É necessário calcular os desdobramentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de desdobramento.
            throw new NotImplementedException();
        }

        private static void CalculateReverseSplitOperation(Movement.EquitMovement movement)
        {
            // É necessário calcular os agrupamentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de agrupamento.
            throw new NotImplementedException();
        }

        private static void CalculateBonusSharesOperation(Movement.EquitMovement movement)
        {
            // É necessário calcular as bonificações de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de bonificação.
            throw new NotImplementedException();
        }
    }
}
