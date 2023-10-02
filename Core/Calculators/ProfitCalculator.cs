using common.Helpers;
using Common.Helpers;
using Core.Constants;
using Core.Models;
using Core.Models.B3;

namespace Core.Calculators
{
    /// <summary>
    /// Responsável por calcular o lucro de movimentações de compra e venda, levando em consideração o preço médio.
    /// Também leva em consideração movimentos de bonificação, desdobramento e agrupamento.
    /// </summary>
    public abstract class ProfitCalculator
    {
        /// <summary>
        /// Retorna o lucro de todas as movimentações especificadas em operações swing-trade e day-trade.
        /// Além disso, atualiza a lista <c>averagePrices</c> com os novos preços médios e, caso algum ativo
        /// tenha sido totalmente vendido, o remove da lista.
        /// </summary>
        public static CalculateProfitResponse CalculateProfit
            (IEnumerable<Movement.EquitMovement> movements, List<AverageTradedPriceDetails> averagePrices)
        
        {
            CalculateProfitResponse investorMovements = new();

            foreach (var movement in movements)
            {
                switch (movement.MovementType)
                {
                    case B3ResponseConstants.Buy:
                        UpdateOrAddAveragePrice(movement, averagePrices, sellOperation: false);
                        AddOperationHistory(movement, investorMovements.OperationHistory);
                        break;
                    case B3ResponseConstants.Sell:
                        AddTickerIntoResponse(investorMovements, movement);
                        UpdateProfitOrLoss(investorMovements, movement, movements, averagePrices);
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

            return investorMovements;
        }

        private static void AddOperationHistory(
            Movement.EquitMovement movement,
            List<OperationDetails> operationsHistory,
            double profit = 0
        )
        {
            operationsHistory.Add(new OperationDetails(
                movement.ReferenceDate.Day,
                UtilsHelper.GetDayOfTheWeekName((int)movement.ReferenceDate.DayOfWeek),
                movement.AssetType,
                AssetEnumHelper.GetEnumByName(movement.AssetType),
                movement.TickerSymbol,
                movement.CorporationName,
                movement.MovementType,
                (int)movement.EquitiesQuantity,
                movement.OperationValue,
                profit
            ));
        }

        private static void AddTickerIntoResponse(
            CalculateProfitResponse investorMovements,
            Movement.EquitMovement movement
        )
        {
            if (TickerAlreadyAdded(investorMovements.DayTradeOperations, investorMovements.SwingTradeOperations, movement)) return;

            if (movement.DayTraded)
                investorMovements.DayTradeOperations.Add(new(movement.TickerSymbol));
            else
                investorMovements.SwingTradeOperations.Add(new(movement.TickerSymbol));
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

        private static void UpdateOrAddAveragePrice(
            Movement.EquitMovement movement,
            List<AverageTradedPriceDetails> averageTradedPrices,
            bool sellOperation
        )
        {
            var ticker = averageTradedPrices.Where(x => x.TickerSymbol == movement.TickerSymbol).FirstOrDefault();

            if (ticker is null)
            {
                averageTradedPrices.Add(new AverageTradedPriceDetails(
                        movement.TickerSymbol,
                        averageTradedPrice: movement.OperationValue / movement.EquitiesQuantity,
                        totalBought: movement.OperationValue,
                        tradedQuantity: (int)movement.EquitiesQuantity
                    ));

                return;
            }

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

            if (InvestorSoldAllTicker(ticker))
            {
                averageTradedPrices.Remove(ticker);
            }
        }

        private static void UpdateProfitOrLoss(
            CalculateProfitResponse investorMovements,
            Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> movements,
            List<AverageTradedPriceDetails> averagePrices
        )
        {
            MovementProperties asset = null!;

            if (movement.DayTraded)
                asset = investorMovements.DayTradeOperations.First(x => x.TickerSymbol == movement.TickerSymbol);
            else
                asset = investorMovements.SwingTradeOperations.First(x => x.TickerSymbol == movement.TickerSymbol);

            if (AssetBoughtAfterB3MinimumDate(movement, averagePrices))
            {
                var averageTradedPrice = averagePrices.Where(x => x.TickerSymbol == movement.TickerSymbol).First();

                double profitPerShare = movement.UnitPrice - averageTradedPrice.AverageTradedPrice;
                double totalProfit = profitPerShare * movement.EquitiesQuantity;

                if (totalProfit > 0)
                {
                    // TODO (MVP?): calcular IRRFs (e.g dedo-duro).
                    // TODO (MVP?): calcular emolumentos.
                }

                asset.Profit = totalProfit;

                UpdateOrAddAveragePrice(movement, averagePrices, sellOperation: true);

                AddOperationHistory(movement, investorMovements.OperationHistory, asset.Profit);
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.

                asset.TickerBoughtBeforeB3DateRange = true;
                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        public static decimal CalculateTaxesFromProfit(double swingTradeProfit, double dayTradeProfit, int aliquot)
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
