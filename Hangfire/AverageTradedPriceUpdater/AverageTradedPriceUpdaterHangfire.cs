using common.Helpers;
using Core.Calculators;
using Core.Models;
using Core.Models.B3;
using Core.Refit.B3;
using Hangfire.AverageTradedPriceUpdater;
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
                if (DateTime.UtcNow.Day != 1) return;

                var accounts = await accountRepository.GetAll();

                string lastMonthFirstDay = GetLastMonthFirstDay();
                string lastMonthFinalDay = GetLastMonthFinalDay();

                // TODO background job? we dont need multithread
                foreach (var account in accounts)
                {
                    Root? lastMonthMovements = null;
#if DEBUG
                    lastMonthMovements = GenerateMockMovements();
#else
                    lastMonthMovements = await client.GetAccountMovement(
                        UtilsHelper.RemoveSpecialCharacters(account.CPF),
                        lastMonthFirstDay,
                        lastMonthFinalDay,
                        account.Id
                    );
#endif

                    if (lastMonthMovements is null || lastMonthMovements.Data is null) continue;

                    var movements = lastMonthMovements.Data.EquitiesPeriods.EquitiesMovements;

                    var lastMonthAverageTradedPrices = await GetLastMonthTradedAverageTradedPrices(movements, account.Id);
                    var allAverageTradedPrices = await averageTradedPriceRepository.GetAverageTradedPrices(account.Id);

                    var _ = CalculateProfitAndAverageTradedPrice(movements, lastMonthAverageTradedPrices);

                    var tickersNamesToAdd = AverageTradedPriceUpdaterHelper.GetTickersToAdd(lastMonthAverageTradedPrices, allAverageTradedPrices);
                    var tickersNamesToUpdate = AverageTradedPriceUpdaterHelper.GetTickersToUpdate(lastMonthAverageTradedPrices, allAverageTradedPrices);
                    var tickersNamesToRemove = AverageTradedPriceUpdaterHelper.GetTickersToRemove(lastMonthAverageTradedPrices, movements);

                    await AddTickers(account,
                        lastMonthAverageTradedPrices.Where(x => tickersNamesToAdd.Any(y => y.Equals(x.TickerSymbol))));

                    await UpdateTickers(account,
                        lastMonthAverageTradedPrices.Where(x => tickersNamesToUpdate.Any(y => y.Equals(x.TickerSymbol))));

                    await RemoveTickers(account.Id, tickersNamesToRemove);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao rodar o job de atualização de preço médio de todos os investidores");
                throw;
            }
        }

        private async Task UpdateTickers(Infrastructure.Models.Account account, IEnumerable<AverageTradedPriceDetails> tickersToUpdate)
        {
            foreach (var t in tickersToUpdate)
            {
                await averageTradedPriceRepository.UpdateAsync(account, new AverageTradedPrice(
                    t.TickerSymbol,
                    t.AverageTradedPrice,
                    t.TotalBought,
                    t.TradedQuantity,
                    account,
                    DateTime.Now
                ));
            }
        }

        private async Task RemoveTickers(Guid accountId, IEnumerable<string> tickersToRemove)
        {
            await averageTradedPriceRepository.RemoveByTickerNameAsync(accountId, tickersToRemove);
        }

        private async Task AddTickers(Infrastructure.Models.Account account, IEnumerable<AverageTradedPriceDetails> tickersToAdd)
        {
            foreach (var t in tickersToAdd)
            {
                await averageTradedPriceRepository.AddAsync(new AverageTradedPrice(
                    t.TickerSymbol,
                    t.AverageTradedPrice,
                    t.TotalBought,
                    t.TradedQuantity,
                    account,
                    DateTime.Now
                ));
            }
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
            int yearInTheLastMonth = DateTime.Now.AddMonths(-1).Year;
            string lastMonth = FormatLastMonth(DateTime.Now.AddMonths(-1).Month);
            int lastMonthLastDay = DateTime.DaysInMonth(yearInTheLastMonth, int.Parse(lastMonth));

            return $"{yearInTheLastMonth}-{lastMonth}-{lastMonthLastDay}";
        }

        /// <summary>
        /// Converte números do <see cref="DateTime.Month"/> de 1 dígito para 2.
        /// </summary>
        /// <param name="month"></param>
        /// <returns>Um número de dois dígitos sendo o primeiro um 0 caso <see cref="DateTime.Month"/> tenha apenas 1 dígito.</returns>
        private static string FormatLastMonth(int month)
        {
            if (month.ToString().Length == 1)
                return $"0{month}";

            return month.ToString();
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
                ProductTypeName = "FII - Fundo de Investimento Imobiliário",
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
                ProductTypeName = "ETF - Exchange Traded Fund",
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
                ProductTypeName = "ETF - Exchange Traded Fund",
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
                ProductTypeName = "Ações",
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
                ProductTypeName = "Ações",
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
                ProductTypeName = "Ações",
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
