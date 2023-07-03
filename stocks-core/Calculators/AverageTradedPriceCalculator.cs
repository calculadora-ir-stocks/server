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
        private static readonly Dictionary<string, AverageTradedPriceDetails> averageTradedPricesList = new();

        public static (List<OperationDetails> dayTrade, List<OperationDetails> swingTrade) CalculateProfit
            (IEnumerable<Movement.EquitMovement> movements, List<AverageTradedPriceDetails>? averageTradedPrices = null)
        {
            List<OperationDetails> dayTrade = new();
            List<OperationDetails> swingTrade = new();

            if (averageTradedPrices != null)
            {
                AddIntoAverageTradedPricesList(averageTradedPrices, Asset.Stocks);
            }

            foreach (var movement in movements)
            {
                switch (movement.MovementType)
                {
                    case B3ResponseConstants.Buy:
                        UpdateAverageTradedPrice(movement);
                        break;
                    case B3ResponseConstants.Sell:
                        UpdateProfit(dayTrade, swingTrade, movement, movements);
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

        public static IEnumerable<Dto> ToDto(IEnumerable<Movement.EquitMovement> movements, string assetType)
        {
            var tradedTickers = movements
                .Where(x => x.AssetType == assetType)
                .Select(x => (x.TickerSymbol, x.CorporationName)).Distinct();

            foreach (var item in tradedTickers) yield return new Dto(item.TickerSymbol, item.CorporationName);
        }

        public static void AddIntoAverageTradedPricesList(List<AverageTradedPriceDetails> averageTradedPrices, Asset assetType)
        {
            foreach (var price in GetAverageTradedPrices(assetType))
            {
                bool contains = averageTradedPrices.Select(x => x.TickerSymbol).Contains(price.TickerSymbol);

                if (contains)
                {
                    AverageTradedPriceDetails averageTradedPrice = averageTradedPrices.Where(x => x.TickerSymbol == price.TickerSymbol).First();
                    averageTradedPrice.UpdateValues(price.TotalBought, price.TradedQuantity);
                }
                else
                {
                    averageTradedPrices.Add(price);
                }
            }
        }

        private static void AddTickerIntoResponseDictionary(
            List<OperationDetails> dayTradeResponse,
            List<OperationDetails> swingTradeResponse,
            Movement.EquitMovement movement
        )
        {
            if (TickerAlreadyAdded(dayTradeResponse, swingTradeResponse, movement)) return;

            if (movement.DayTraded)
                dayTradeResponse.Add(new OperationDetails(movement.TickerSymbol, movement.CorporationName));

            if (!movement.DayTraded)
                swingTradeResponse.Add(new OperationDetails(movement.TickerSymbol, movement.CorporationName));
        }

        private static bool TickerAlreadyAdded(List<OperationDetails> dayTradeResponse, List<OperationDetails> swingTradeResponse, Movement.EquitMovement movement)
        {
            return dayTradeResponse.Select(x => x.TickerSymbol).Equals(movement.TickerSymbol) ||
                swingTradeResponse.Select(x => x.TickerSymbol).Equals(movement.TickerSymbol);
        }

        private static void UpdateAverageTradedPrice(Movement.EquitMovement movement)
        {
            bool tickerHasAverageTradedPrice = averageTradedPricesList.ContainsKey(movement.TickerSymbol);

            if (tickerHasAverageTradedPrice)
            {
                var ticker = averageTradedPricesList[movement.TickerSymbol];

                double totalBought = ticker.TotalBought + movement.OperationValue;
                double quantity = ticker.TradedQuantity + movement.EquitiesQuantity;

                ticker.UpdateValues(totalBought, (int)quantity);
            }
            else
            {
                averageTradedPricesList.Add(movement.TickerSymbol, new AverageTradedPriceDetails(
                    movement.CorporationName,
                    averageTradedPrice: movement.OperationValue / movement.EquitiesQuantity,
                    totalBought: movement.OperationValue,
                    tradedQuantity: (int)movement.EquitiesQuantity,
                    AssetTypeHelper.GetAssetTypeByName(movement.AssetType)
                ));
            }
        }

        protected static IEnumerable<AverageTradedPriceDetails> GetAverageTradedPrices(Asset assetType)
        {
            var prices = averageTradedPricesList.Where(x => x.Value.AssetType == assetType);

            foreach (var price in prices)
            {
                yield return new AverageTradedPriceDetails(
                    price.Key,
                    price.Value.AverageTradedPrice,
                    price.Value.TotalBought,
                    price.Value.TradedQuantity,
                    assetType
                );
            }
        }

        private static void UpdateProfit(
            List<OperationDetails> dayTradeResponse,
            List<OperationDetails> swingTradeResponse,
            Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> movements
        )
        {
            AddTickerIntoResponseDictionary(dayTradeResponse, swingTradeResponse, movement);

            OperationDetails? asset = null;

            if (movement.DayTraded)
                asset = dayTradeResponse.First(x => x.TickerSymbol == movement.TickerSymbol);
            else
                asset = swingTradeResponse.First(x => x.TickerSymbol == movement.TickerSymbol);

            if (AssetBoughtAfterB3MinimumDate(movement))
            {
                double averageTradedPrice = averageTradedPricesList[movement.TickerSymbol].AverageTradedPrice;
                double profitPerShare = movement.UnitPrice - averageTradedPrice;
                double totalProfit = profitPerShare * movement.EquitiesQuantity;

                if (totalProfit > 0)
                {
                    // TO-DO (MVP?): calcular IRRFs (e.g dedo-duro).
                }

                asset.UpdateTotalProfit(totalProfit);

                // TO-DO (MVP?): calcular emolumentos.
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.

                asset.UpdateTickerBoughtBeforeB3DateRange(boughtBeforeB3DateRange: true);
                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        private static bool AssetBoughtAfterB3MinimumDate(Movement.EquitMovement movement)
        {
            return averageTradedPricesList.ContainsKey(movement.TickerSymbol);
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

    public class Dto
    {
        public Dto(string ticker, string corporationName)
        {
            Ticker = ticker;
            CorporationName = corporationName;
        }

        public string Ticker { get; init; }
        public string CorporationName { get; init; }
    }
}
