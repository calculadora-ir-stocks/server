using Microsoft.IdentityModel.Tokens;
using stocks.Models;
using stocks.Repositories;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Enums;
using stocks_core.Response;
using stocks_infrastructure.Models;

namespace stocks_core.Business
{
    /// <summary>
    /// Classe responsável por calcular o imposto de renda a ser pago em todos os ativos de 01/11/2019 até D-1.
    /// </summary>
    public class BigBang
    {
        private readonly IGenericRepository<IncomeTaxes> _genericRepositoryIncomeTaxes;
        private readonly IGenericRepository<Account> _genericRepositoryAccount;

        public BigBang(IGenericRepository<IncomeTaxes> genericRepositoryIncomeTaxes, IGenericRepository<Account> genericRepositoryAccount)
        {
            _genericRepositoryIncomeTaxes = genericRepositoryIncomeTaxes;
            _genericRepositoryAccount = genericRepositoryAccount;
        }

        public async Task Calculate(Movement.Root response, IIncomeTaxesCalculator calculator, Guid accountId)
        {
            var movements = GetAllMovements(response);
            if (movements.IsNullOrEmpty()) return;

            movements = OrderMovements(movements);

            var month = movements[0].ReferenceDate.ToString("MM/yyyy");
            List<MonthMovement> monthMovements = new();

            foreach(var movement in movements)
            {
                var currentMovementMonth = movement.ReferenceDate.ToString("MM/yyyy");

                if (currentMovementMonth == month)
                {
                    AddMovementsForEachMonth(movement, monthMovements);
                } else
                {
                    month = currentMovementMonth;
                    AddMovementsForEachMonth(movement, monthMovements);
                }
            }

            await CalculateEachMonth(monthMovements, calculator, accountId);
        }

        private static List<Movement.EquitMovement> GetAllMovements(Movement.Root response)
        {
            return response.Data.EquitiesPeriods.EquitiesMovements
                .Where(x => 
                    x.MovementType.Equals(B3ServicesConstants.Buy) ||
                    x.MovementType.Equals(B3ServicesConstants.Sell) ||
                    x.MovementType.Equals(B3ServicesConstants.Split) ||
                    x.MovementType.Equals(B3ServicesConstants.ReverseSplit) ||
                    x.MovementType.Equals(B3ServicesConstants.BonusShare)).ToList();
        }

        /// <summary>
        /// Ordena as operações por ordem crescente através da data - a B3 retorna em ordem decrescente - e
        /// ordena operações de compra antes das operações de venda em operações day trade.
        /// </summary>
        private static List<Movement.EquitMovement> OrderMovements(IList<Movement.EquitMovement> movements)
        {
            return movements.OrderBy(x => x.MovementType).OrderBy(x => x.ReferenceDate).ToList();
        }

        private async Task CalculateEachMonth(List<MonthMovement> monthMovements, IIncomeTaxesCalculator calculator, Guid accountId)
        {
            Dictionary<string, CalculateAssetsIncomeTaxesResponse> response = new();

            foreach (var monthMovement in monthMovements)
            {
                response.Add(monthMovement.Month, new CalculateAssetsIncomeTaxesResponse());

                var stocks = monthMovement.Movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Stocks));
                var etfs = monthMovement.Movements.Where(x => x.AssetType.Equals(AssetMovementTypes.ETFs));
                var fiis = monthMovement.Movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FIIs));
                var bdrs = monthMovement.Movements.Where(x => x.AssetType.Equals(AssetMovementTypes.BDRs));
                var gold = monthMovement.Movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Gold));
                var fundInvestments = monthMovement.Movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FundInvestments));

                if (stocks.Any())
                {
                    calculator = new StocksIncomeTaxes();
                    calculator.CalculateIncomeTaxesForAllMonths(response[monthMovement.Month], stocks);
                }

                if (etfs.Any())
                {
                    calculator = new ETFsIncomeTaxes();
                    calculator.CalculateCurrentMonthIncomeTaxes(response[monthMovement.Month], etfs, accountId);
                }

                if (fiis.Any())
                {
                    //calculator = new FIIsIncomeTaxes();
                    //await calculator.AddAllIncomeTaxesToObject(response, fiis, accountId);
                }

                if (bdrs.Any())
                {
                    //calculator = new BDRsIncomeTaxes();
                    //await calculator.AddAllIncomeTaxesToObject(response, bdrs, accountId);
                }

                if (gold.Any())
                {
                    //calculator = new GoldIncomeTaxes();
                    //await calculator.AddAllIncomeTaxesToObject(response, gold, accountId);
                }

                if (fundInvestments.Any())
                {
                    //calculator = new FundInvestmentsIncomeTaxes();
                    //await calculator.AddAllIncomeTaxesToObject(response, fundInvestments, accountId);
                }
            }

            await SaveIntoDatabase(response, accountId);
        }

        private async Task SaveIntoDatabase(Dictionary<string, CalculateAssetsIncomeTaxesResponse> response, Guid accountId)
        {
            Account account = _genericRepositoryAccount.GetById(accountId);

            foreach (var item in response.Values)
            {
                IncomeTaxes incomeTaxes = new
                (
                    month: "a",
                    totalTaxes: item.TotalIncomeTaxesValue,
                    totalSold: item.TotalSold,
                    totalProfit: item.TotalProfit,
                    dayTraded: item.DayTraded,
                    tradedAssets: item.Assets!,
                    compesatedLoss: item.TotalProfit < 0 ? false : null,
                    accountId: accountId,
                    account: account
                );

                await _genericRepositoryIncomeTaxes.AddAsync(incomeTaxes);
            }
        }

        private static void AddMovementsForEachMonth(Movement.EquitMovement movement, List<MonthMovement> monthMovements)
        {
            var monthAlreadyExists = 
                monthMovements.Where(m => m.Month == movement.ReferenceDate.ToString("MM/yyyy")).FirstOrDefault();

            if (monthAlreadyExists is null)
            {
                MonthMovement monthMovement = new(movement.ReferenceDate.ToString("MM/yyyy"));

                monthMovement.Movements.Add(new Movement.EquitMovement(
                    movement.TickerSymbol,
                    movement.CorporationName,
                    movement.AssetType,
                    movement.MovementType,
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.UnitPrice,
                    movement.ReferenceDate)
                );

                monthMovements.Add(monthMovement);
            } else
            {
                monthAlreadyExists.Movements.Add(new Movement.EquitMovement(
                    movement.TickerSymbol,
                    movement.CorporationName,
                    movement.AssetType,
                    movement.MovementType,
                    movement.OperationValue,
                    movement.EquitiesQuantity,
                    movement.UnitPrice,
                    movement.ReferenceDate)
                );
            }
        }

        /// <summary>
        /// Representa todas as operações em um determinado mês.
        /// </summary>
        private class MonthMovement
        {
            public MonthMovement(string month)
            {
                Month = month;
            }

            public string Month;
            public List<Movement.EquitMovement> Movements = new();
        }
    }
}

