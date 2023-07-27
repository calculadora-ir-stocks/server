using common.Helpers;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Calculators
{
    /// <summary>
    /// Responsável por calcular o lucro de movimentações de compra e venda, levando em consideração o preço médio.
    /// Também leva em consideração movimentos de bonificação, desdobramento e agrupamento.
    /// </summary>
    public abstract class ProfitCalculator
    {
        private static List<OperationDetails>? operationDetails = null;

        /// <summary>
        /// Retorna o lucro de todas as movimentações especificadas em operações swing-trade e day-trade.
        /// </summary>
        public static (List<MovementProperties> dayTrade, List<MovementProperties> swingTrade) Calculate
            (IEnumerable<Movement.EquitMovement> movements, List<AverageTradedPriceDetails> movementsAverageTradedPrices)
        {
            List<MovementProperties> dayTrade = new();
            List<MovementProperties> swingTrade = new();

            operationDetails = new();

            foreach (var movement in movements)
            {
                switch (movement.MovementType)
                {
                    case B3ResponseConstants.Buy:
                        UpdateAverageTradedPrice(movement, movementsAverageTradedPrices!);
                        AddMovementDetails(movement);
                        break;
                    case B3ResponseConstants.Sell:
                        AddTickerIntoResponseDictionary(dayTrade, swingTrade, movement);
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

        private static void AddMovementDetails(Movement.EquitMovement movement, double profit = 0)
        {
            if (operationDetails is null) throw new NullReferenceException();

            operationDetails.Add(new OperationDetails(
                movement.ReferenceDate.Day.ToString(),
                DayOfTheWeekHelper.GetDayOfTheWeek((int)movement.ReferenceDate.DayOfWeek),
                movement.TickerSymbol,
                movement.CorporationName,
                movement.MovementType,
                (int)movement.EquitiesQuantity,
                movement.OperationValue,
                profit
            ));
        }

        protected static List<OperationDetails> GetOperationDetails()
        {
            if (operationDetails is null) throw new NullReferenceException();
            return operationDetails;
        }

        private static void AddTickerIntoResponseDictionary(
            List<MovementProperties> dayTradeResponse,
            List<MovementProperties> swingTradeResponse,
            Movement.EquitMovement movement
        )
        {
            if (TickerAlreadyAdded(dayTradeResponse, swingTradeResponse, movement)) return;

            if (movement.DayTraded)
                dayTradeResponse.Add(new(movement.TickerSymbol));
            else
                swingTradeResponse.Add(new(movement.TickerSymbol));
        }

        private static bool TickerAlreadyAdded(List<MovementProperties> dayTradeResponse, List<MovementProperties> swingTradeResponse, Movement.EquitMovement movement)
        {
            return dayTradeResponse.Select(x => x.TickerSymbol).Equals(movement.TickerSymbol) ||
                swingTradeResponse.Select(x => x.TickerSymbol).Equals(movement.TickerSymbol);
        }

        private static bool InvestorSoldAllTicker(AverageTradedPriceDetails ticker)
        {
            return ticker.TradedQuantity == 0;
        }

        private static bool AssetBoughtAfterB3MinimumDate(Movement.EquitMovement movement, List<AverageTradedPriceDetails> averageTradedPrices)
        {
            return averageTradedPrices.Where(x => x.TickerSymbol == movement.TickerSymbol).FirstOrDefault() != null;
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

                if (InvestorSoldAllTicker(ticker)) averageTradedPrices.Remove(ticker);
            }
            else
            {
                averageTradedPrices.Add(new AverageTradedPriceDetails(
                    movement.TickerSymbol,
                    averageTradedPrice: movement.OperationValue / movement.EquitiesQuantity,
                    totalBought: movement.OperationValue,
                    tradedQuantity: (int)movement.EquitiesQuantity
                ));
            }
        }

        private static void UpdateProfitOrLoss(
            List<MovementProperties> dayTrade,
            List<MovementProperties> swingTrade,
            Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> movements,
            List<AverageTradedPriceDetails> averageTradedPrices
        )
        {
            MovementProperties? asset = null;

            if (movement.DayTraded)
                asset = dayTrade.First(x => x.TickerSymbol == movement.TickerSymbol);
            else
                asset = swingTrade.First(x => x.TickerSymbol == movement.TickerSymbol);

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

                asset.UpdateProfit(totalProfit);

                UpdateAverageTradedPrice(movement, averageTradedPrices, sellOperation: true);
                AddMovementDetails(movement, asset.Profit);
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.

                asset.UpdateTickerBoughtBeforeB3DateRange(boughtBeforeB3DateRange: true);
                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
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
