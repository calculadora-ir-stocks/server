﻿using Microsoft.Extensions.Logging;
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

        public async Task Insert(Guid accountId)
        {
            try
            {
                if (AccountAlreadyHasAverageTradedPrice(accountId)) return;

                string minimumAllowedStartDateByB3 = "2019-11-01";
                string referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                // Movement.Root? response = await _client.GetAccountMovement("97188167044", minimumAllowedStartDateByB3, referenceEndDate)!;
                Movement.Root? response = new();
                response.Data = new();
                response.Data.EquitiesPeriods = new();
                response.Data.EquitiesPeriods.EquitiesMovements = new();

                response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
                {
                    AssetType = "Ações",
                    TickerSymbol = "PETR4",
                    MovementType = "Compra",
                    OperationValue = 10.43,
                    EquitiesQuantity = 2,
                });

                response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
                {
                    AssetType = "Ações",
                    TickerSymbol = "PETR4",
                    MovementType = "Compra",
                    OperationValue = 13.12,
                    EquitiesQuantity = 5,
                });

                response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
                {
                    AssetType = "Ações",
                    TickerSymbol = "PETR4",
                    MovementType = "Venda",
                    OperationValue = 9.32,
                    EquitiesQuantity = 3,
                });

                var buyOperations = response.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == "Compra");

                var sellOperations = response.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Sell);
                
                var splitsOperations = response.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Split);

                Dictionary<string, AverageTradedPriceCalculator> averageTradedPrices = CalculateAverageTradedPrice(buyOperations, sellOperations, splitsOperations);
                List<stocks_infrastructure.Models.AverageTradedPrice> mappedAverageTradedPrices = new();

                foreach (KeyValuePair<string, AverageTradedPriceCalculator> entry in averageTradedPrices)
                {
                    mappedAverageTradedPrices.Add(new stocks_infrastructure.Models.AverageTradedPrice
                    {
                        Ticker = entry.Key,
                        AveragePrice = entry.Value.AverageTradedPrice,
                        Quantity = (int)entry.Value.CurrentQuantity,
                        AccountId = accountId,
                        UpdatedAt = DateTime.UtcNow,
                    });
                }

                _averageTradedPriceRepository.InsertAll(mappedAverageTradedPrices);
            }
            catch (Exception e)
            {
                _logger.LogError("Uma exceção ocorreu ao executar o método {1}, classe {2}. Exceção: {3}",
                    nameof(Insert), nameof(AverageTradedPriceService), e.Message);
                throw;
            }
        }

        public static Dictionary<string, AverageTradedPriceCalculator> CalculateAverageTradedPrice(
            IEnumerable<Movement.EquitMovement> buyOperations,
            IEnumerable<Movement.EquitMovement> sellOperations,
            IEnumerable<Movement.EquitMovement> splitsOperations
        )
        {
            Dictionary<string, AverageTradedPriceCalculator> total = new();

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

                    asset.CurrentPrice += movement.OperationValue;
                    asset.CurrentQuantity += movement.EquitiesQuantity;
                } else
                {
                    total.Add(movement.TickerSymbol, new AverageTradedPriceCalculator(movement.OperationValue, movement.EquitiesQuantity));
                }
            }

            foreach (var movement in sellOperations)
            {
                if (total.ContainsKey(movement.TickerSymbol))
                {
                    var asset = total[movement.TickerSymbol];

                    asset.CurrentPrice -= movement.OperationValue;
                    asset.CurrentQuantity -= movement.EquitiesQuantity;

                    if (asset.CurrentQuantity == 0) total.Remove(movement.TickerSymbol);
                }
            }

            foreach(var movement in total)
            {
                var totalSpent = movement.Value.CurrentPrice;
                var assetQuantity = movement.Value.CurrentQuantity;

                movement.Value.AverageTradedPrice = FormatToTwoDecimalPlaces(totalSpent / assetQuantity);
            }

            return total;
        }

        private static double FormatToTwoDecimalPlaces(double value)
        {
            return Math.Truncate((100 * (value))) / 100;
        }

        private bool AccountAlreadyHasAverageTradedPrice(Guid accountId)
        {
            return _averageTradedPriceRepository.AccountAlreadyHasAverageTradedPrice(accountId);
        }
    }
}
