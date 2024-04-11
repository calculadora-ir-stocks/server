using Api.Clients.B3;
using common.Helpers;
using Core.Calculators;
using Core.Models;
using Core.Models.B3;
using Infrastructure.Dtos;
using Infrastructure.Models;
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

                // TODO background job? we dont need multithread
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
                    var allAverageTradedPrices = await averageTradedPriceRepository.GetAverageTradedPrices(account.Id);

                    var _ = CalculateProfitAndAverageTradedPrice(movements, lastMonthAverageTradedPrices);

                    var tickersNamesToUpdate = GetTickersToUpdate(lastMonthAverageTradedPrices, allAverageTradedPrices);
                    var tickersNamesToAdd = GetTickersToAdd(lastMonthAverageTradedPrices, allAverageTradedPrices);
                    var tickersNamesToRemove = GetTickerToRemove(lastMonthAverageTradedPrices, movements);

                    await AddTickers(lastMonthAverageTradedPrices.Where(x => x.TickerSymbol.Equals(tickersNamesToAdd)), account);
                    await UpdateTickers(lastMonthAverageTradedPrices.Where(x => x.TickerSymbol.Equals(tickersNamesToAdd)), account);
                    await RemoveTickers(tickersNamesToRemove);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao rodar o job de atualização de preço médio de todos os investidores");
                throw;
            }
        }

        private async Task RemoveTickers(IEnumerable<string> tickersNamesToRemove)
        {
            throw new NotImplementedException();
        }

        private async Task UpdateTickers(IEnumerable<AverageTradedPriceDetails> averageTradedPrices, Infrastructure.Models.Account account)
        {
            throw new NotImplementedException();
        }

        private async Task AddTickers(IEnumerable<AverageTradedPriceDetails> averageTradedPrices, Infrastructure.Models.Account account)
        {
            foreach (var a in averageTradedPrices)
            {
                await averageTradedPriceRepository.AddAsync(new AverageTradedPrice(
                    a.TickerSymbol,
                    a.AverageTradedPrice,
                    a.TotalBought,
                    a.TradedQuantity,
                    account,
                    DateTime.Now
                ));
            }
        }

        private static IEnumerable<string> GetTickersToUpdate(List<AverageTradedPriceDetails> lastMonthAverageTradedPrices,
            IEnumerable<AverageTradedPriceDto> allTickers)
        {

            var tickers = lastMonthAverageTradedPrices.Where(x => allTickers.Any(y => y.Ticker.Equals(x.TickerSymbol)));
            return tickers.Select(x => x.TickerSymbol);
        }

        private static IEnumerable<string> GetTickersToAdd(List<AverageTradedPriceDetails> lastMonthAverageTradedPrices,
            IEnumerable<AverageTradedPriceDto> allTickers)
        {
            return lastMonthAverageTradedPrices.Select(x => x.TickerSymbol).ToList().Except(allTickers.Select(x => x.Ticker));
        }

        private static IEnumerable<string> GetTickerToRemove(List<AverageTradedPriceDetails> lastMonthAverageTradedPrices, List<EquitMovement> movements)
        {
            return movements.Where(m => !lastMonthAverageTradedPrices.Any(l => l.TickerSymbol == m.TickerSymbol)).Select(x => x.TickerSymbol);
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

            // ticker to update
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "BOVA11",
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
