using Microsoft.IdentityModel.Tokens;
using stocks_common.Exceptions;
using stocks_common.Models;
using stocks_core.Calculators;
using stocks_core.Calculators.Assets;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Services.BigBang
{
    /// <summary>
    /// Classe responsável por calcular o imposto de renda devido nos meses especificados.
    /// </summary>
    public class BigBang : IBigBang
    {
        private IIncomeTaxesCalculator calculator;

        public BigBang(IIncomeTaxesCalculator calculator)
        {
            this.calculator = calculator;
        }

        /// <summary>
        /// Calcula o imposto de renda a ser pago em todos os meses especificados no parâmetro de retorno da B3.
        /// </summary>
        public (List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>) Execute(Movement.Root? request)
        {
            var movements = GetAllInvestorMovements(request);
            if (movements.IsNullOrEmpty()) throw new NoneMovementsException("O usuário não possui nenhuma movimentação na bolsa até então.");

            movements = OrderMovementsByDateAndMovementType(movements);

            var monthlyMovements = new Dictionary<string, List<Movement.EquitMovement>>();
            var monthsThatHadMovements = movements.Select(x => x.ReferenceDate.ToString("MM/yyyy")).Distinct();

            foreach (var month in monthsThatHadMovements)
            {
                var monthMovements = movements.Where(x => x.ReferenceDate.ToString("MM/yyyy") == month).ToList();
                monthlyMovements.Add(month, monthMovements);
            }

            return GetTaxesAndAverageTradedPrices(monthlyMovements);
        }

        private static List<Movement.EquitMovement> GetAllInvestorMovements(Movement.Root? response)
        {
            if (response is null || response.Data is null) return new List<Movement.EquitMovement>();

            var movements = response.Data.EquitiesPeriods.EquitiesMovements;

            return movements
                .Where(x =>
                    x.MovementType.Equals(B3ResponseConstants.Buy) ||
                    x.MovementType.Equals(B3ResponseConstants.Sell) ||
                    x.MovementType.Equals(B3ResponseConstants.Split) ||
                    x.MovementType.Equals(B3ResponseConstants.ReverseSplit) ||
                    x.MovementType.Equals(B3ResponseConstants.BonusShare))
                .ToList();
        }

        /// <summary>
        /// Ordena as operações por ordem crescente através da data - a B3 retorna em ordem decrescente - e
        /// ordena operações de compra antes das operações de venda em operações day trade.
        /// </summary>
        private List<Movement.EquitMovement> OrderMovementsByDateAndMovementType(IList<Movement.EquitMovement> movements)
        {
            // TODO: a premissa da descrição do método está correta?
            return movements.OrderBy(x => x.MovementType).OrderBy(x => x.ReferenceDate).ToList();
        }

        private (List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>) GetTaxesAndAverageTradedPrices(Dictionary<string, List<Movement.EquitMovement>> monthlyMovements)
        {
            List<AssetIncomeTaxes> assetsIncomeTaxes = new();
            List<AverageTradedPriceDetails> averageTradedPrices = new();

            foreach (var monthMovements in monthlyMovements)
            {
                SetDayTradeSellOperations(monthMovements.Value);

                var stocks = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.Stocks));
                var etfs = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.ETFs));
                var fiis = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.FIIs));
                var bdrs = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.BDRs));
                var gold = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.Gold));
                var fundInvestments = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.InvestmentsFunds));

                if (stocks.Any())
                {
                    calculator = new StocksIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes, averageTradedPrices, stocks, monthMovements.Key);
                }

                if (etfs.Any())
                {
                    calculator = new ETFsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes, averageTradedPrices, etfs, monthMovements.Key);
                }

                if (fiis.Any())
                {
                    calculator = new FIIsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes, averageTradedPrices, fiis, monthMovements.Key);
                }

                if (bdrs.Any())
                {
                    calculator = new BDRsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes, averageTradedPrices, bdrs, monthMovements.Key);
                }

                if (gold.Any())
                {
                    calculator = new GoldIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes, averageTradedPrices, gold, monthMovements.Key);
                }

                if (fundInvestments.Any())
                {
                    calculator = new InvestmentsFundsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes, averageTradedPrices, fundInvestments, monthMovements.Key);
                }
            }

            return (assetsIncomeTaxes, averageTradedPrices);
        }

        /// <summary>
        /// Altera a propriedade booleana DayTraded para operações de venda day-trade.
        /// </summary>
        private static void SetDayTradeSellOperations(List<Movement.EquitMovement> movements)
        {
            var buys = movements.Where(x => x.MovementType == B3ResponseConstants.Buy);
            var sells = movements.Where(x => x.MovementType == B3ResponseConstants.Sell);

            var dayTradeSellsOperationsIds = sells.Where(b => buys.Any(s =>
                s.ReferenceDate == b.ReferenceDate &&
                s.TickerSymbol == b.TickerSymbol
            )).Select(x => x.Id);

            foreach(var id in dayTradeSellsOperationsIds)
            {
                var dayTradeOperation = movements.Where(x => x.Id == id).Single();
                dayTradeOperation.DayTraded = true;
            }
        }
    }
}
