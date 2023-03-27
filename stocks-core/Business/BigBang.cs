using stocks_core.DTOs.B3;
using stocks_core.Enums;
using stocks_core.Response;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Business
{
    /// <summary>
    /// Classe responsável por calcular o imposto de renda a ser pago em todos os ativos de 01/11/2019 até D-1.
    /// </summary>
    public class BigBang
    {
        private readonly IAverageTradedPriceRepostory _averageTradedPriceRepository;
        private readonly IAverageTradedPriceService _averageTradedPriceService;

        public BigBang(IAverageTradedPriceRepostory averageTradedPriceRepository, IAverageTradedPriceService averageTradedPriceService)
        {
            _averageTradedPriceRepository = averageTradedPriceRepository;
            _averageTradedPriceService = averageTradedPriceService;
        }

        public async Task Calculate(Movement.Root response, IIncomeTaxesCalculator calculator, Guid accountId)
        {
            var movements = response.Data.EquitiesPeriods.EquitiesMovements;
            if (movements is null) return;

            var month = movements[0].ReferenceDate.ToString("MM/yyyy");
            List<MonthMovement> monthMovements = new();

            foreach(var movement in movements)
            {
                var currentMovementMonth = movement.ReferenceDate.ToString("MM/YYYY");

                if (currentMovementMonth == month)
                {
                    AddMovementsForEachMonth(movement, monthMovements);
                } else
                {
                    month = currentMovementMonth;
                    AddMovementsForEachMonth(movement, monthMovements);
                }
            }

            await CalculateAndSaveIntoDatabase(monthMovements, calculator, accountId);
        }

        private async Task CalculateAndSaveIntoDatabase(List<MonthMovement> monthMovements, IIncomeTaxesCalculator calculator, Guid accountId)
        {
            foreach (var monthMovement in monthMovements)
            {
                Dictionary<string, CalculateAssetsIncomeTaxesResponse> response = new();
                response.Add(monthMovement.Month, new CalculateAssetsIncomeTaxesResponse());

                var movements = monthMovement.Movements;

                var stocks = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Stocks));
                var etfs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.ETFs));
                var fiis = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FIIs));
                var bdrs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.BDRs));
                var gold = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Gold));
                var fundInvestments = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FundInvestments));

                if (stocks.Any())
                {
                    calculator = new StocksIncomeTaxes(_averageTradedPriceRepository, _averageTradedPriceService);
                    await calculator.CalculateIncomeTaxesForTheFirstTimeAndSaveAverageTradedPrice(response[monthMovement.Month], stocks, accountId);
                }

                if (etfs.Any())
                {
                    calculator = new ETFsIncomeTaxes();
                    await calculator.AddAllIncomeTaxesToObject(response[monthMovement.Month], etfs, accountId);
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
        }

        private static void AddMovementsForEachMonth(Movement.EquitMovement movement, List<MonthMovement> monthMovements)
        {
            var monthAlreadyExists = 
                monthMovements.Where(m => m.Month == movement.ReferenceDate.ToString("MM/yyyy")).FirstOrDefault();

            if (monthAlreadyExists is null)
            {
                MonthMovement monthMovement = new();

                monthMovement.Month = movement.ReferenceDate.ToString("MM/yyyy");

                monthMovement.Movements.Add(new Movement.EquitMovement(
                    movement.TickerSymbol,
                    movement.AssetType,
                    movement.MovementType,
                    movement.OperationValue,
                    movement.EquitiesQuantity)
                );

                monthMovements.Add(monthMovement);
            } else
            {
                monthAlreadyExists.Movements.Add(new Movement.EquitMovement(
                    movement.TickerSymbol,
                    movement.AssetType,
                    movement.MovementType,
                    movement.OperationValue,
                    movement.EquitiesQuantity)
                );
            }
        }

        /// <summary>
        /// Representa todas as operações em um determinado mês.
        /// </summary>
        private class MonthMovement
        {
            public string Month;
            public List<Movement.EquitMovement> Movements = new();
        }
    }
}

