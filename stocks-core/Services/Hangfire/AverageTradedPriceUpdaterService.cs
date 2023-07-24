using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using stocks.Clients.B3;
using stocks.Repositories.Account;
using stocks_common.Models;
using stocks_core.Calculators;
using stocks_core.DTOs.B3;
using stocks_infrastructure.Dtos;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Services.Hangfire
{
    public class AverageTradedPriceUpdaterService : AverageTradedPriceCalculator, IAverageTradedPriceUpdaterService
    {
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;
        private readonly IAccountRepository accountRepository;
        private readonly IB3Client client;
        private readonly ILogger<AverageTradedPriceUpdaterService> logger;

        public AverageTradedPriceUpdaterService
        (
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IAccountRepository accountRepository,
            IB3Client client,
            ILogger<AverageTradedPriceUpdaterService> logger
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
                var accounts = accountRepository.GetAllAccounts();

                foreach (var account in accounts)
                {
                    Stopwatch timer = new();
                    timer.Start();

                    string lastMonthFirstDay = GetLastMonthFirstDay();
                    string lastMonthFinalDay = GetLastMonthFinalDay();

                    // var lastMonthMovements = await client.GetAccountMovement(account.CPF, lastMonthFirstDay, lastMonthFinalDay, account.Id);

                    Movement.Root? mockData = new()
                    {
                        Data = new()
                        {
                            EquitiesPeriods = new()
                            {
                                EquitiesMovements = new()
                            }
                        }
                    };

                    GenerateMockMovements(mockData);

                    var movements = mockData.Data.EquitiesPeriods.EquitiesMovements;
                    var averageTradedPrices = await GetTradedAverageTradedPrices(movements, account.Id);

                    List<AverageTradedPriceDetails> updatedAverageTradedPrices = new();
                    updatedAverageTradedPrices.AddRange(ToDtoAverageTradedPriceDetails(averageTradedPrices));

                    var (_, _) = CalculateProfit(movements, updatedAverageTradedPrices);

                    var tickersToAddIntoDatabase = await GetTradedTickersToAddIntoDatabase(updatedAverageTradedPrices, account);
                    var tickersToUpdateFromDatabase = GetTradedTickersToUpdate(tickersToAddIntoDatabase!, updatedAverageTradedPrices, account);
                    var tickersToRemoveFromDatabase = GetTradedTickersToRemove(movements, updatedAverageTradedPrices);

                    await UpdateInvestorAverageTradedPrices(
                        tickersToAddIntoDatabase,
                        tickersToUpdateFromDatabase,
                        updatedAverageTradedPrices,
                        tickersToRemoveFromDatabase,
                        account
                    );

                    timer.Stop();
                    var timeTakenForEach = timer.Elapsed;

                    logger.LogInformation("Tempo de execução do investidor {id}: {timeTaken}.",
                        account.Id,
                        timeTakenForEach
                    );
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
            }
        }

        /// <summary>
        /// Adiciona, atualiza e remove os preços médios da base de dados de um determinado
        /// investidor.
        /// </summary>
        private async Task UpdateInvestorAverageTradedPrices(
            IEnumerable<AverageTradedPrice>? tickersToAdd,
            IEnumerable<AverageTradedPrice>? tickersToUpdate,
            IEnumerable<AverageTradedPriceDetails> updatedAverageTradedPrices,
            IEnumerable<string?> tickersToRemove,
            Account account
        )
        {
            if (!tickersToAdd.IsNullOrEmpty())
                await AddTradedTickers(tickersToAdd!);

            if (!tickersToUpdate.IsNullOrEmpty())
                await UpdateTradedTickers(tickersToUpdate!, updatedAverageTradedPrices);

            if (!tickersToRemove.IsNullOrEmpty())
                await RemoveTradedTickers(tickersToRemove!, account.Id);
        }

        private static IEnumerable<AverageTradedPriceDetails> ToDtoAverageTradedPriceDetails(
            IEnumerable<AverageTradedPriceDto> averageTradedPrices
        )
        {
            foreach (var item in averageTradedPrices)
            {
                yield return new AverageTradedPriceDetails(
                    item.Ticker,
                    averageTradedPrice: item.AverageTradedPrice,
                    totalBought: item.AverageTradedPrice,
                    item.Quantity
                );
            }
        }

        private async Task<IEnumerable<AverageTradedPriceDto>> GetTradedAverageTradedPrices(
            List<Movement.EquitMovement> movements, Guid id
        )
        {
            return await averageTradedPriceRepository.GetAverageTradedPricesDto(id, movements.Select(x => x.TickerSymbol).ToList());
        }

        private async Task RemoveTradedTickers(IEnumerable<string?> tickersToRemove, Guid id)
        {
            await averageTradedPriceRepository.RemoveAllAsync(tickersToRemove, id);
        }

        private async Task UpdateTradedTickers(
            IEnumerable<AverageTradedPrice> tickersToUpdate,
            IEnumerable<AverageTradedPriceDetails> updatedAverageTradedPrices
        )
        {
            foreach (var item in tickersToUpdate)
            {
                var updatedTicker = updatedAverageTradedPrices.Where(x => x.TickerSymbol == item.Ticker).First();

                item.Quantity = updatedTicker.TradedQuantity;
                item.AveragePrice = updatedTicker.AverageTradedPrice;
                item.UpdatedAt = DateTime.Now;
            }

            await averageTradedPriceRepository.UpdateAllAsync(tickersToUpdate.ToList());
        }

        private async Task AddTradedTickers(IEnumerable<AverageTradedPrice> tickersToAdd)
        {
            await averageTradedPriceRepository.AddAllAsync(tickersToAdd.ToList());
        }

        private static IEnumerable<string?> GetTradedTickersToRemove(
            List<Movement.EquitMovement> movements, List<AverageTradedPriceDetails> updatedPrices
        )
        {
            if (movements is null) return Array.Empty<string>();

            var currentMonthTickers = movements.Select(x => x.TickerSymbol);

            // O método CalculateProfit() exclui da lista de preços médios os tickers que foram totalmente vendidos.
            // Nesse caso, comparar os tickers negociados no mês com os tickers que não estão no response é o suficiente para obter os tickers
            // que precisam ser removidos da base de dados.
            return currentMonthTickers.Except(updatedPrices.Select(x => x.TickerSymbol));
        }

        /// <summary>
        /// Retorna os tickers que necessitam ser atualizados e já estão inseridos na base.
        /// Para isso, compara os tickers que precisam ser adicionados com os tickers que foram negociados.
        /// </summary>
        private List<AverageTradedPrice>? GetTradedTickersToUpdate(
            IEnumerable<AverageTradedPrice> tickersToAdd, List<AverageTradedPriceDetails> updatedPrices, Account account
        )
        {
            var tickersToUpdate = updatedPrices.Select(x => x.TickerSymbol).Except(tickersToAdd.Select(x => x.Ticker)).ToList();
            return averageTradedPriceRepository.GetAverageTradedPrices(account.Id, tickersToUpdate);
        }

        private async Task<IEnumerable<AverageTradedPrice>?> GetTradedTickersToAddIntoDatabase(
            List<AverageTradedPriceDetails> tradedAssets, Account account
        )
        {
            var tickersInvestorAlreadyHas =
                await averageTradedPriceRepository.GetAverageTradedPricesDto(account.Id, tradedAssets.Select(x => x.TickerSymbol).ToList());

            var tickersInvestorDoesntHave =
            tradedAssets.Select(x => x.TickerSymbol).ToList().Except(tickersInvestorAlreadyHas.Select(x => x.Ticker));

            // To dto
            List<AverageTradedPrice> response = new();

            foreach (var item in tickersInvestorDoesntHave)
            {
                AverageTradedPriceDetails asset = tradedAssets.Where(x => x.TickerSymbol == item).First();

                response.Add(new AverageTradedPrice(
                    asset.TickerSymbol,
                    asset.AverageTradedPrice,
                    asset.TradedQuantity,
                    account,
                    DateTime.Now
                ));
            }

            return response;
        }

        private static void GenerateMockMovements(Movement.Root response)
        {
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
                EquitiesQuantity = 1,
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
        }

        private static string GetLastMonthFinalDay()
        {
            var date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return date.AddDays(-1).ToString("yyyy-MM-dd");
        }

        private static string GetLastMonthFirstDay()
        {
            var date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
            return date.AddMonths(-1).ToString("yyyy-MM-01");
        }
    }
}
