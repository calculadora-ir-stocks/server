using Microsoft.IdentityModel.Tokens;
using stocks.Clients.B3;
using stocks.Repositories.Account;
using stocks_common.Models;
using stocks_core.DTOs.B3;
using stocks_core.Services.IncomeTaxes;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Services.Hangfire
{
    public class AverageTradedPriceUpdaterService : IAverageTradedPriceUpdaterService
    {
        private readonly IIncomeTaxesService incomeTaxesService;
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;
        private readonly IAccountRepository accountRepository;
        private readonly IB3Client client;

        public AverageTradedPriceUpdaterService
        (
            IIncomeTaxesService incomeTaxesService,
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IAccountRepository accountRepository,
            IB3Client client
        )
        {
            this.incomeTaxesService = incomeTaxesService;
            this.averageTradedPriceRepository = averageTradedPriceRepository;
            this.accountRepository = accountRepository;
            this.client = client;
        }

        public async Task Execute()
        {
            var accounts = accountRepository.GetAllAccounts();

            // TO-DO: multithread
            foreach (var account in accounts)
            {
                string lastMonthFirstDay = GetLastMonthFirstDay();
                string lastMonthFinalDay = GetLastMonthFinalDay();

                // var lastMonthMovements =
                // await client.GetAccountMovement(account.Item2, lastMonthFirstDay, lastMonthFinalDay, account.Item1);

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

                var response = await incomeTaxesService.Execute(mockData, account.Id);
                var tradedTickers = response.Item2;

                var tickersToAdd = await GetTradedTickersToAdd(tradedTickers, account.Id);
                var tickersToUpdate = GetTradedTickersToUpdate(tickersToAdd!, tradedTickers);
                var tickersToRemove = GetTradedTickersToRemove(mockData, tradedTickers);

                if (!tickersToAdd.IsNullOrEmpty()) 
                    await AddTradedTickers(tickersToAdd!, account);
                 
                if (!tickersToUpdate.IsNullOrEmpty())
                    await UpdateTradedTickers(tickersToUpdate!, account);

                if (!tickersToRemove.IsNullOrEmpty())
                    await RemoveTradedTickers(tickersToRemove!, account.Id);
            }
        }

        private async Task RemoveTradedTickers(IEnumerable<string?> tickersToRemove, Guid id)
        {
            await averageTradedPriceRepository.RemoveAllAsync(tickersToRemove, id);
        }

        private async Task UpdateTradedTickers(IEnumerable<AverageTradedPriceDetails> tickersToUpdate, Account account)
        {
            List<AverageTradedPrice> pricesToUpdate = new();

            foreach (var item in tickersToUpdate)
            {
                pricesToUpdate.Add(new AverageTradedPrice(
                    item.TickerSymbol,
                    item.AverageTradedPrice,
                    item.TradedQuantity,
                    account,
                    updatedAt: DateTime.Now
                ));
            }

            await averageTradedPriceRepository.UpdateAllAsync(pricesToUpdate);
        }

        private async Task AddTradedTickers(IEnumerable<AverageTradedPriceDetails> tickersToAdd, Account account)
        {
            List<AverageTradedPrice> pricesToAdd = new();

            foreach (var item in tickersToAdd)
            {
                pricesToAdd.Add(new AverageTradedPrice(
                    item.TickerSymbol,
                    item.AverageTradedPrice,
                    item.TradedQuantity,
                    account,
                    updatedAt: DateTime.Now
                ));
            }

            await averageTradedPriceRepository.AddAllAsync(pricesToAdd);
        }

        private IEnumerable<string?> GetTradedTickersToRemove(Movement.Root lastMonthMovements, List<AverageTradedPriceDetails> tradedTickers)
        {
            if (lastMonthMovements is null) return Array.Empty<string>();

            var movements = lastMonthMovements.Data.EquitiesPeriods.EquitiesMovements;
            var tickers = movements.Select(x => x.TickerSymbol);

            // O método _incomeTaxesService.Execute exclui da lista de preços médios os tickers que foram completamente vendidos.
            // Nesse caso, comparar os tickers negociados no mês com os tickers que não estão no response é o suficiente para obter os tickers
            // que precisam ser removidos da base de dados.
            return tickers.Except(tradedTickers.Select(x => x.TickerSymbol));
        }

        private IEnumerable<AverageTradedPriceDetails?> GetTradedTickersToUpdate(IEnumerable<AverageTradedPriceDetails?> tradedTickersToAdd, List<AverageTradedPriceDetails> tradedTickers)
        {
            if (tradedTickersToAdd is null) return tradedTickers;

            return tradedTickers.Except(tradedTickersToAdd);
        }

        private async Task<IEnumerable<AverageTradedPriceDetails>?> GetTradedTickersToAdd(List<AverageTradedPriceDetails> tradedAssets, Guid item1)
        {
            var tickersSymbol = tradedAssets.Select(x => x.TickerSymbol);
            var tickersInvestorAlreadyHas = 
                await averageTradedPriceRepository.GetAverageTradedPrices(item1, tickersSymbol.ToList());

            var tickersSymbolInvestorAlreadyHas = tickersInvestorAlreadyHas.Select(x => x.Ticker);
            var tickersInvestorDoesntHave = tickersSymbol.Except(tickersSymbolInvestorAlreadyHas);

            List<AverageTradedPriceDetails> response = new();

            foreach (var item in tickersInvestorDoesntHave)
            {
                AverageTradedPriceDetails asset = tradedAssets.Where(x => x.TickerSymbol == item).First();
                response.Add(asset);
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
                OperationValue = 604.43,
                EquitiesQuantity = 1,
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
