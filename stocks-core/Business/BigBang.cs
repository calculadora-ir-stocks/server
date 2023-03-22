using stocks_core.DTOs.B3;
using stocks_core.Enums;
using stocks_core.Response;

namespace stocks_core.Business
{
    /// <summary>
    /// Classe responsável por calcular o imposto de renda a ser pago em todos os ativos de 01/11/2019 até D-1.
    /// </summary>
    public static class BigBang
    {
        public static async Task Calculate(Movement.Root response, IIncomeTaxesCalculator calculator, Guid accountId)
        {
            var movements = response.Data.EquitiesPeriods.EquitiesMovements;
            if (movements is null) return;

            var month = movements[0].ReferenceDate.ToString("MM");
            List<MonthMovement> monthMovements = new();

            foreach(var movement in movements)
            {
                var currentMovementMonth = movement.ReferenceDate.ToString("MM");

                if (currentMovementMonth == month)
                {
                    AddMovementsToEachMonth(movement, monthMovements);
                } else
                {
                    month = currentMovementMonth;
                    AddMovementsToEachMonth(movement, monthMovements);
                }
            }

            CalculateAndSaveIntoDatabase(monthMovements, calculator, accountId);
        }

        private static void CalculateAndSaveIntoDatabase(List<MonthMovement> monthMovements, IIncomeTaxesCalculator calculator, Guid accountId)
        {

            foreach (var monthMovement in monthMovements)
            {
                // Month and income taxes to be paid.
                Dictionary<int, CalculateAssetsIncomeTaxesResponse> response = new();
                
                //var movements = monthMovement.Movements;

                //var stocks = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Stocks));
                //var etfs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.ETFs));
                //var fiis = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FIIs));
                //var bdrs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.BDRs));
                //var gold = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Gold));
                //var fundInvestments = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FundInvestments));

                //calculator = new StocksIncomeTaxes(_averageTradedPriceRepository, _averageTradedPriceService);
                //await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, stocks, accountId);

                //_incomeTaxCalculator = new ETFsIncomeTaxes();
                //await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, etfs, accountId);

                //// _incomeTaxCalculator = new FIIsIncomeTaxes();
                //await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, fiis, accountId);

                //// _incomeTaxCalculator = new BDRsIncomeTaxes();
                //await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, bdrs, accountId);

                //// _incomeTaxCalculator = new GoldIncomeTaxes();
                //await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, gold, accountId);

                //// _incomeTaxCalculator = new FundInvestmentsIncomeTaxes();
                //await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, fundInvestments, accountId);
            }
        }

        private static void AddMovementsToEachMonth(Movement.EquitMovement movement, List<MonthMovement> monthMovements)
        {
            var monthAlreadyExists = 
                monthMovements.Where(m => m.Month == int.Parse(movement.ReferenceDate.ToString("MM"))).FirstOrDefault();

            if (monthAlreadyExists is null)
            {
                MonthMovement monthMovement = new();

                monthMovement.Month = int.Parse(movement.ReferenceDate.ToString("MM"));

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
            public int Month;
            public List<Movement.EquitMovement> Movements = new();
        }
    }
}

