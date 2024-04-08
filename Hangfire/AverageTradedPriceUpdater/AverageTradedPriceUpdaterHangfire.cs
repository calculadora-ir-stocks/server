using Api.Clients.B3;
using common.Helpers;
using Core.Calculators;
using Core.Models;
using Core.Models.B3;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Microsoft.Extensions.Logging;
using static Core.Models.B3.Movement;

namespace Core.Services.Hangfire.AverageTradedPriceUpdater
{
    public class AverageTradedPriceUpdaterHangfire : ProfitCalculator, IAverageTradedPriceUpdaterHangfire
    {
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;
        private readonly IAccountRepository accountRepository;
        private readonly IB3Client client;
        private readonly ILogger<AverageTradedPriceUpdaterHangfire> logger;

        public AverageTradedPriceUpdaterHangfire
        (
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IAccountRepository accountRepository,
            IB3Client client,
            ILogger<AverageTradedPriceUpdaterHangfire> logger
        )
        {
            this.averageTradedPriceRepository = averageTradedPriceRepository;
            this.accountRepository = accountRepository;
            this.client = client;
            this.logger = logger;
        }

        public async Task Execute()
        {
            try
            {
                var accounts = accountRepository.GetAll();

                string lastMonthFirstDay = GetLastMonthFirstDay();
                string lastMonthFinalDay = GetLastMonthFinalDay();

                foreach (var account in accounts)
                {
                    //var lastMonthMovements = await client.GetAccountMovement(
                    //    UtilsHelper.RemoveSpecialCharacters(account.CPF),
                    //    lastMonthFirstDay,
                    //    lastMonthFinalDay,
                    //    account.Id
                    //);

                    var lastMonthMovements = GenerateMockMovements();

                    if (lastMonthMovements is null || lastMonthMovements.Data is null) continue;

                    var movements = lastMonthMovements.Data.EquitiesPeriods.EquitiesMovements;
                    var lastMonthAverageTradedPrices = await GetLastMonthTradedAverageTradedPrices(movements, account.Id);

                    var tickersNamesToUpdate = GetTickersToUpdate(movements, lastMonthAverageTradedPrices);

                    // CalculateProfitAndAverageTradedPrice remove os tickers totalmente vendidos (e que logo devem ser removidos da base)
                    // da lista "lastMonthAverageTradedPrices".
                    var _ = CalculateProfitAndAverageTradedPrice(movements, lastMonthAverageTradedPrices);

                    var tickersNamesToAdd = await GetTickersToAdd(lastMonthAverageTradedPrices, account.Id);
                    var tickersNamesToRemove = GetTickerToRemove(lastMonthAverageTradedPrices, lastMonthAverageTradedPrices);

                    await AddTickers(movements, tickersNamesToAdd);
                    await UpdateTickers(movements, tickersNamesToUpdate);
                    await RemoveTickers(movements, tickersNamesToRemove);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao rodar o job de atualização de preço médio de todos os investidores");
                throw;
            }
        }

        private Task RemoveTickers(List<EquitMovement> movements, IEnumerable<string> tickersNamesToRemove)
        {
            throw new NotImplementedException();
        }

        private Task UpdateTickers(List<EquitMovement> movements, IEnumerable<string> tickersNamesToUpdate)
        {
            throw new NotImplementedException();
        }

        private Task AddTickers(List<EquitMovement> movements, IEnumerable<string> tickersNamesToAdd)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<string> GetTickerToRemove(List<AverageTradedPriceDetails> averageTradedPrices, List<AverageTradedPriceDetails> averageTradedPricesBeforeUpdate)
        {
            return averageTradedPricesBeforeUpdate.Where(p => averageTradedPrices.All(p2 => p2.TickerSymbol != p.TickerSymbol)).Select(x => x.TickerSymbol);
        }

        private static IEnumerable<string> GetTickersToUpdate(List<EquitMovement> movements, List<AverageTradedPriceDetails> averageTradedPrices)
        {
            var test = movements.Where(x => averageTradedPrices.Any(y => y.TickerSymbol.Equals(x.TickerSymbol)));
            return test.Select(x => x.TickerSymbol);
        }

        private async Task<IEnumerable<string>> GetTickersToAdd(List<AverageTradedPriceDetails> lastMonthAverageTradedPrices, Guid accountId)
        {
            var allTickers = await averageTradedPriceRepository.GetAverageTradedPrices(accountId);
            var newTickers = lastMonthAverageTradedPrices.Select(x => x.TickerSymbol).ToList().Except(allTickers.Select(x => x.Ticker));

            return newTickers;
        }

        private async Task<List<AverageTradedPriceDetails>> GetLastMonthTradedAverageTradedPrices(
            List<EquitMovement> movements, Guid id
        )
        {
            var response = await averageTradedPriceRepository.GetAverageTradedPrices(id, movements.Select(x => x.TickerSymbol).ToList());

            return response
                .Select(x => new AverageTradedPriceDetails(x.Ticker, x.AverageTradedPrice, x.TotalBought, x.Quantity))
                .ToList();
        }

        private static string GetLastMonthFinalDay()
        {
            var yearInTheLastMonth = DateTime.Now.AddMonths(-1).Year;
            // TODO o lastMonth tá retornando '3' ao invés de '03' pra mês. A API da B3 aceita?
            var lastMonth = DateTime.Now.AddMonths(-1).Month;
            var lastMonthLastDay = DateTime.DaysInMonth(yearInTheLastMonth, lastMonth);

            return $"{yearInTheLastMonth}-{lastMonth}-{lastMonthLastDay}";
        }

        private static string GetLastMonthFirstDay()
        {
            return DateTime.Now.AddMonths(-1).ToString("yyyy-MM-01");
        }

        private static Root GenerateMockMovements()
        {
            Root? response = new()
            {
                Data = new()
                {
                    EquitiesPeriods = new()
                    {
                        EquitiesMovements = new()
                    }
                }
            };

            // ticker to add
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "FII - Fundo de Investimento Imobiliário",
                TickerSymbol = "KFOF11",
                CorporationName = "KFOF11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 231.34,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2022, 01, 16)
            });

            // ticker to update
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "IVVB11",
                CorporationName = "IVVB 11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 12376.43,
                EquitiesQuantity = 4,
                ReferenceDate = new DateTime(2022, 01, 09)
            });

            // ticker to remove
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "AMER3",
                CorporationName = "Americanas S/A",
                MovementType = "Venda",
                OperationValue = 234.43,
                UnitPrice = 234.43,
                EquitiesQuantity = 2,
                ReferenceDate = new DateTime(2022, 02, 01)
            });

            // don't do anything
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "DONT",
                CorporationName = "Americanas S/A",
                MovementType = "Compra",
                OperationValue = 234.43,
                UnitPrice = 234.43,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2022, 02, 01)
            });

            // don't do anything
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "DONT",
                CorporationName = "Americanas S/A",
                MovementType = "Venda",
                OperationValue = 234.43,
                UnitPrice = 234.43,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2022, 02, 01)
            });

            return response;
        }
    }
}
