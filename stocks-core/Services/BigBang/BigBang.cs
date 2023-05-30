using Microsoft.IdentityModel.Tokens;
using stocks_common.Exceptions;
using stocks_core.Calculators;
using stocks_core.Calculators.Assets;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Services.BigBang
{
    public class BigBang : IBigBang
    {
        private IIncomeTaxesCalculator calculator;

        public BigBang(IIncomeTaxesCalculator calculator)
        {
            this.calculator = calculator;
        }

        public Dictionary<string, List<AssetIncomeTaxes>> Calculate(Movement.Root? request)
        {
            var movements = GetAllInvestorMovements(request);
            if (movements.IsNullOrEmpty()) throw new NoneMovementsException("O usuário não possui nenhuma movimentação na bolsa até então.");

            OrderMovementsByDateAndMovementType(movements);

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
            if (response is null) return new List<Movement.EquitMovement>();

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
        private static void OrderMovementsByDateAndMovementType(IList<Movement.EquitMovement> movements)
        {
            movements = movements.OrderBy(x => x.MovementType).OrderBy(x => x.ReferenceDate).ToList();
        }

        private Dictionary<string, List<AssetIncomeTaxes>> GetTaxesAndAverageTradedPrices(Dictionary<string, List<Movement.EquitMovement>> monthlyMovements)
        {
            Dictionary<string, List<AssetIncomeTaxes>> assetsIncomeTaxes = new();

            foreach (var monthMovements in monthlyMovements)
            {
                assetsIncomeTaxes.Add(monthMovements.Key, new List<AssetIncomeTaxes>());

                var stocks = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.Stocks));
                var etfs = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.ETFs));
                var fiis = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.FIIs));
                var bdrs = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.BDRs));
                var gold = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.Gold));
                var fundInvestments = monthMovements.Value.Where(x => x.AssetType.Equals(B3ResponseConstants.FundInvestments));

                if (stocks.Any())
                {
                    calculator = new StocksIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes[monthMovements.Key], stocks);
                }

                if (etfs.Any())
                {
                    calculator = new ETFsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes[monthMovements.Key], etfs);
                }

                if (fiis.Any())
                {
                    calculator = new FIIsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes[monthMovements.Key], fiis);
                }

                if (bdrs.Any())
                {
                    calculator = new BDRsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes[monthMovements.Key], bdrs);
                }

                if (gold.Any())
                {
                    calculator = new GoldIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes[monthMovements.Key], gold);
                }

                if (fundInvestments.Any())
                {
                    calculator = new InvestmentsFundsIncomeTaxes();
                    calculator.CalculateIncomeTaxesForSpecifiedMovements(assetsIncomeTaxes[monthMovements.Key], fundInvestments);
                }
            }

            return assetsIncomeTaxes;
        }
    }
}
