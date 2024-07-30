using Azure;
using common.Helpers;
using Common.Helpers;
using Core.Constants;
using Core.Models;
using Core.Models.B3;
using System.Data;

namespace Core.Calculators
{
    /// <summary>
    /// Responsável por calcular o lucro de movimentações de compra e venda, levando em consideração o preço médio.
    /// Também leva em consideração movimentos de bonificação, desdobramento e agrupamento.
    /// </summary>
    public abstract class ProfitCalculator
    {
        /// <summary>
        /// Retorna o lucro ou prejuízo de todas as movimentações especificadas em operações swing-trade e day-trade.
        /// Além disso, atualiza a lista <c>averagePrices</c> com os novos preços médios e, caso algum ativo
        /// tenha sido totalmente vendido, o remove da lista.
        /// </summary>
        public static CalculateProfitResponse CalculateProfitAndAverageTradedPrice(IEnumerable<Movement.EquitMovement> movements, List<AverageTradedPriceDetails> averagePrices)
        {
            CalculateProfitResponse response = new();

            foreach (var movement in movements)
            {
                if (movement.TickerSymbol.Contains("XPSF11"))
                {
                    Console.WriteLine("Movement above \n");
                    Console.WriteLine(movement);
                }

                if (movement.IsBuy())
                {
                    UpdateOrAddAveragePrice(movement, averagePrices, sellOperation: false);
                    response.OperationHistory.Add(CreateOperationDetails(movement));
                    continue;
                }
                if (movement.IsSell())
                {
                    AddTickerIntoHistoryList(response, movement);
                    UpdateProfitOrLoss(response, movement, movements, averagePrices);
                    continue;
                }

                if (AssetBoughtBeforeB3MinimumDate(movement, averagePrices))
                {
                    response.TickersBoughtBeforeB3Range.Add(movement.TickerSymbol);
                    continue;
                }

                switch (movement.MovementType)
                {
                    case B3ResponseConstants.Split:
                        CalculateSplitOperation(movement, ticker: averagePrices.Where(x => x.TickerSymbol.Equals(movement.TickerSymbol)).First());
                        break;
                    case B3ResponseConstants.ReverseSplit:
                        CalculateReverseSplitOperation(movement, ticker: averagePrices.Where(x => x.TickerSymbol.Equals(movement.TickerSymbol)).First());
                        break;
                    case B3ResponseConstants.BonusShare:
                        CalculateBonusSharesOperation(movement, ticker: averagePrices.Where(x => x.TickerSymbol.Equals(movement.TickerSymbol)).First());
                        break;
                }
            }

            return response;
        }

        private static bool AssetBoughtBeforeB3MinimumDate(Movement.EquitMovement movement, List<AverageTradedPriceDetails> averagePrices)
        {
            var ticker = averagePrices.Where(x => x.TickerSymbol.Equals(movement.TickerSymbol)).FirstOrDefault();
            return ticker is null;
        }

        private static OperationDetails CreateOperationDetails(
            Movement.EquitMovement movement,
            double profit = 0
        )
        {
            string operationType = string.Empty;

            if (movement.IsBuy())
                operationType = "Compra";
            else if (movement.IsSell())
                operationType = "Venda";

            return new OperationDetails(
                movement.ReferenceDate.Day,
                UtilsHelper.GetDayOfTheWeekName((int)movement.ReferenceDate.DayOfWeek),
                movement.ProductTypeName,
                AssetEnumHelper.GetEnumByName(movement.ProductTypeName),
                movement.TickerSymbol,
                movement.CorporationName,
                operationType,
                (int)movement.EquitiesQuantity,
                movement.OperationValue,
                profit
            );
        }

        private static void AddTickerIntoHistoryList(
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

        private static bool TickerAlreadyAdded(List<MovementProperties> dayTradeOperations, List<MovementProperties> swingTradeOperations, Movement.EquitMovement movement)
        {
            if (movement.DayTraded)
                return dayTradeOperations.Any(x => x.TickerSymbol.Contains(movement.TickerSymbol));
            else
                return swingTradeOperations.Any(x => x.TickerSymbol.Contains(movement.TickerSymbol));
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
                quantity = ticker.TradedQuantity - movement.EquitiesQuantity;
                ticker.UpdateQuantity((int)quantity);

                // Vendeu todos os ativos
                if (ticker.TradedQuantity == 0)
                {
                    averageTradedPrices.Remove(ticker);
                }
            }
            else
            {
                totalBought = ticker.TotalBought + movement.OperationValue;
                quantity = ticker.TradedQuantity + movement.EquitiesQuantity;
                ticker.UpdateAllProperties(totalBought, (int)quantity);
            }            
        }

        private static void UpdateProfitOrLoss(
            CalculateProfitResponse response,
            Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> movements,
            List<AverageTradedPriceDetails> averagePrices
        )
        {
            MovementProperties asset = null!;

            if (movement.DayTraded)
                asset = response.DayTradeOperations.First(x => x.TickerSymbol.Equals(movement.TickerSymbol));
            else
                asset = response.SwingTradeOperations.First(x => x.TickerSymbol.Equals(movement.TickerSymbol));

            if (AssetBoughtAfterB3MinimumDate(movement, averagePrices))
            {
                var averageTradedPrice = averagePrices.Where(x => x.TickerSymbol.Equals(movement.TickerSymbol)).First();

                double profitPerShare = movement.UnitPrice - averageTradedPrice.AverageTradedPrice;
                double totalProfit = profitPerShare * movement.EquitiesQuantity;

                if (totalProfit > 0)
                {
                    // TODO (MVP?): calcular IRRFs (e.g dedo-duro).
                    // TODO (MVP?): calcular emolumentos.
                }

                asset.Profit += totalProfit;

                UpdateOrAddAveragePrice(movement, averagePrices, sellOperation: true);
                response.OperationHistory.Add(CreateOperationDetails(movement, asset.Profit));
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.

                asset.TickerBoughtBeforeB3DateRange = true;
                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        public static decimal CalculateTaxesFromProfit(double profit, bool isDayTrade, int aliquot)
        {
            decimal taxes = 0;

            if (profit > 0)
            {
                if (isDayTrade)
                    taxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)profit;
                else
                    taxes = (aliquot / 100m) * (decimal)profit;
            }

            return taxes;
        }

        private static void CalculateSplitOperation(Movement.EquitMovement movement, AverageTradedPriceDetails ticker)
        {
            // Segundo a B3, o desdobramento retornará a quantidade de ativos que serão adicionados na conta de um investidor.
            // O preço médio é alterado após um desdobramento. Para calculá-lo, multiplique o total gasto no ativo e divida pela nova quantidade de ativos.
            int quantityAddedIntoPortfolio = (int)movement.EquitiesQuantity;
            double newQuantity = quantityAddedIntoPortfolio + ticker.TradedQuantity;

            ticker.UpdateAllProperties(ticker.TotalBought, (int)newQuantity);
        }

        private static void CalculateReverseSplitOperation(Movement.EquitMovement movement, AverageTradedPriceDetails ticker)
        {
            // Segundo a B3, o grupamento retornará a nova quantidade de ativos que o investidor terá em sua posição.
            // O preço médio é alterado após um grupamento. Para calculá-lo, multiplique o total gasto no ativo e divida pela nova quantidade de ativos.
            ticker.UpdateAllProperties(ticker.TotalBought, (int)movement.EquitiesQuantity);
        }

        private static void CalculateBonusSharesOperation(Movement.EquitMovement movement, AverageTradedPriceDetails ticker)
        {
            // Segundo a B3, a bonificação retornará a quantidade de ativos que serão adicionados na conta de um investidor.
            // O preço médio é alterado após uma bonificação. Para calculá-lo, multiplique o total gasto no ativo e divida pela nova quantidade de ativos.
            int quantityAddedIntoPortfolio = (int)movement.EquitiesQuantity;
            double newQuantity = quantityAddedIntoPortfolio + ticker.TradedQuantity;

            ticker.UpdateAllProperties(ticker.TotalBought, (int)newQuantity);
        }
    }
}
