using Microsoft.Extensions.Logging;
using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks_core.Constants;
using stocks_core.DTOs.AverageTradedPrice;
using stocks_core.DTOs.B3;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Services.AverageTradedPrice
{
    public class AverageTradedPriceService : IAverageTradedPriceService
    {
        private readonly IGenericRepository<Account> _genericAccountRepository;
        private readonly IAverageTradedPriceRepostory _averageTradedPriceRepository;

        private readonly IB3Client _client;

        private readonly ILogger<AverageTradedPriceService> _logger;

        public AverageTradedPriceService(IGenericRepository<Account> genericAccountRepository,
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IB3Client client,
            ILogger<AverageTradedPriceService> logger)
        {
            _genericAccountRepository = genericAccountRepository;
            _averageTradedPriceRepository = averageTradedPriceRepository;
            _client = client;

            _logger = logger;
        }

        public Dictionary<string, AverageTradedPriceCalculatorResponse> CalculateAverageTradedPrice(
            IEnumerable<Movement.EquitMovement> buyOperations,
            IEnumerable<Movement.EquitMovement> sellOperations,
            IEnumerable<Movement.EquitMovement> splitsOperations,
            IEnumerable<Movement.EquitMovement> bonusSharesOperations
        )
        {
            Dictionary<string, AverageTradedPriceCalculatorResponse> total = new();

            // TODO: calcular os desdobramentos

            // É necessário verificar quando houve desdobramentos, pois caso não faça,
            // a quantidade de papéis de um ativo pode aumentar e a relação de preço/quantidade
            // estará incorreta.

            // TODO: calcular bonificações

            foreach (var split in splitsOperations)
            {

            }  

            foreach (var movement in buyOperations)
            {
                if (total.ContainsKey(movement.TickerSymbol))
                {
                    var asset = total[movement.TickerSymbol];

                    asset.CurrentPrice += movement.OperationValue * movement.EquitiesQuantity;
                    asset.CurrentQuantity += movement.EquitiesQuantity;
                } else
                {
                    total.Add(movement.TickerSymbol, new AverageTradedPriceCalculatorResponse(movement.OperationValue * movement.EquitiesQuantity, movement.EquitiesQuantity));
                }
            }

            foreach (var movement in sellOperations)
            {
                if (total.ContainsKey(movement.TickerSymbol))
                {
                    var asset = total[movement.TickerSymbol];

                    asset.CurrentPrice -= movement.OperationValue * movement.EquitiesQuantity;
                    asset.CurrentQuantity -= movement.EquitiesQuantity;
                }
            }

            foreach (var movement in total)
            {
                var totalSpent = movement.Value.CurrentPrice;
                var assetQuantity = movement.Value.CurrentQuantity;

                var averageTradedPrice = totalSpent / assetQuantity;

                movement.Value.AverageTradedPrice = FormatToTwoDecimalPlaces(averageTradedPrice);
            }

            return total;
        }

        private static double FormatToTwoDecimalPlaces(double value)
        {
            return Math.Truncate((100 * (value))) / 100;
        }
    }
}
