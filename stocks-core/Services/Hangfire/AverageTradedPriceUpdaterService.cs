using stocks.Clients.B3;
using stocks.Repositories.Account;
using stocks_core.Calculators;
using stocks_core.DTOs.B3;
using stocks_core.Services.IncomeTaxes;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Services.Hangfire
{
    public class AverageTradedPriceUpdaterService : IAverageTradedPriceUpdaterService
    {
        private readonly IIncomeTaxesService _incomeTaxesService;
        private readonly IAverageTradedPriceRepostory _averageTradedPriceRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IB3Client _client;

        public AverageTradedPriceUpdaterService
        (
            IIncomeTaxesService incomeTaxesService,
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IAccountRepository accountRepository,
            IB3Client client
        )
        {
            _incomeTaxesService = incomeTaxesService;
            _averageTradedPriceRepository = averageTradedPriceRepository;
            _accountRepository = accountRepository;
            _client = client;
        }

        public async Task Execute()
        {
            var accountInfo = _accountRepository.GetAllIdsAndCpf();

            // TO-DO: multithread
            foreach (var account in accountInfo)
            {
                string lastMonthFirstDay = GetLastMonthFirstDay();
                string lastMonthFinalDay = GetLastMonthFinalDay();

                // var lastMonthMovements =
                // await _client.GetAccountMovement(account.Item2, lastMonthFirstDay, lastMonthFinalDay, account.Item1);

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

                var response = await _incomeTaxesService.Execute(mockData, account.Item1);
                var tradedTickers = response.Item2;

                await _averageTradedPriceRepository.UpdateTickers(account.Item1, tradedTickers);
            }
        }

        private static void GenerateMockMovements(Movement.Root response)
        {
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "BOVA11",
                CorporationName = "BOVA 11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 10.43,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2022, 01, 01)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "IVVB11",
                CorporationName = "IVVB 11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 245.65,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2022, 01, 09)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "IVVB11",
                CorporationName = "IVVB 11 Corporation Inc.",
                MovementType = "Venda",
                OperationValue = 304.54,
                UnitPrice = 304.54,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2022, 01, 10)
            });

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

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "FII - Fundo de Investimento Imobiliário",
                TickerSymbol = "KFOF11",
                CorporationName = "KFOF11 Corporation Inc.",
                MovementType = "Venda",
                OperationValue = 237.34,
                UnitPrice = 237.34,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2022, 01, 28)
            });

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

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "AMER3",
                MovementType = "Compra",
                CorporationName = "Americanas S/A",
                OperationValue = 265.54,
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
