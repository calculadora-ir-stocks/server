using Api.Clients.B3;
using common.Helpers;
using Core.Calculators;
using Core.Models;
using Core.Models.B3;
using Infrastructure.Models;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
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
                Guid threadId = new();

                logger.LogInformation("Iniciando Hangfire para atualizar o preço médio de todos os investidores." +
                    "Id do processo: {id}", threadId);


                var accounts = accountRepository.GetAll();

                Stopwatch timer = new();
                timer.Start();

                foreach (var account in accounts)
                {
                    string lastMonthFirstDay = GetLastMonthFirstDay();
                    string lastMonthFinalDay = GetLastMonthFinalDay();

                    var lastMonthMovements = await client.GetAccountMovement(
                        UtilsHelper.RemoveSpecialCharacters(account.CPF),
                        lastMonthFirstDay,
                        lastMonthFinalDay,
                        account.Id
                    );

                    if (lastMonthMovements is null) return;

                    var movements = lastMonthMovements.Data.EquitiesPeriods.EquitiesMovements;
                    var averageTradedPrices = await GetMonthTradedAverageTradedPrices(movements, account.Id);

                    var _ = CalculateProfit(movements, averageTradedPrices);

                    var tickersToAddIntoDatabase = await GetTradedTickersToAddIntoDatabase(averageTradedPrices, account);
                    var tickersToUpdateFromDatabase = GetTradedTickersToUpdate(tickersToAddIntoDatabase!, averageTradedPrices, account);
                    var tickersToRemoveFromDatabase = GetTradedTickersToRemove(movements, averageTradedPrices);

                    await UpdateInvestorAverageTradedPrices(
                        tickersToAddIntoDatabase,
                        tickersToUpdateFromDatabase,
                        averageTradedPrices,
                        tickersToRemoveFromDatabase,
                        account
                    );
                }

                timer.Stop();
                var timeTaken = timer.Elapsed;

                logger.LogInformation("Finalizado Hangfire para atualizar o preço médio de todos os investidores." +
                    "Tempo de execução: {timeTaken}. Id do processo: {id}", timeTaken, threadId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ocorreu um erro ao rodar o job de atualização de preço médio de todos os investidores");
                throw;
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
            Infrastructure.Models.Account account
        )
        {
            if (!tickersToAdd.IsNullOrEmpty())
                await AddTradedTickers(tickersToAdd!);

            if (!tickersToUpdate.IsNullOrEmpty())
                await UpdateTradedTickers(tickersToUpdate!, updatedAverageTradedPrices);

            if (!tickersToRemove.IsNullOrEmpty())
                await RemoveTradedTickers(tickersToRemove!, account.Id);
        }

        private async Task<List<AverageTradedPriceDetails>> GetMonthTradedAverageTradedPrices(
            List<Movement.EquitMovement> movements, Guid id
        )
        {
            var response = await averageTradedPriceRepository.GetAverageTradedPricesDto(id, movements.Select(x => x.TickerSymbol).ToList());

            return response
                .Select(x => new AverageTradedPriceDetails(x.Ticker, x.AverageTradedPrice, x.AverageTradedPrice, x.Quantity))
                .ToList();
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
            if (movements.IsNullOrEmpty()) return Array.Empty<string>();

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
            IEnumerable<AverageTradedPrice> tickersToAdd, List<AverageTradedPriceDetails> updatedPrices, Infrastructure.Models.Account account
        )
        {
            var tickersToUpdate = updatedPrices.Select(x => x.TickerSymbol).Except(tickersToAdd.Select(x => x.Ticker)).ToList();
            return averageTradedPriceRepository.GetAverageTradedPrices(account.Id, tickersToUpdate);
        }

        private async Task<IEnumerable<AverageTradedPrice>?> GetTradedTickersToAddIntoDatabase(
            List<AverageTradedPriceDetails> tradedAssets, Infrastructure.Models.Account account
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

            return response;
        }

        private static string GetLastMonthFinalDay()
        {
            // Como o Job é executado todo o dia 01, substrair um dia resultará no último dia do mês passado.
            return DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        }

        private static string GetLastMonthFirstDay()
        {
            return DateTime.Now.AddMonths(-1).ToString("yyyy-MM-01");
        }
    }
}
