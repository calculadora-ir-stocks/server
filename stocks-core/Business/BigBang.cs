using stocks_core.DTOs.B3;
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
            CalculateAssetsIncomeTaxesResponse? response = null;

            foreach (var monthMovement in monthMovements)
            {
                calculator.AddAllIncomeTaxesToObject(response, monthMovement.Movements, accountId);
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

