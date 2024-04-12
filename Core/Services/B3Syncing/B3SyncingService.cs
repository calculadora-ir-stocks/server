using Api.Clients.B3;
using Common.Enums;
using Common.Exceptions;
using Common.Helpers;
using Core.Models;
using Core.Requests.BigBang;
using Core.Services.B3ResponseCalculator;
using Infrastructure.Models;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.Taxes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Core.Services.B3Syncing
{
    public class B3SyncingService : IB3SyncingService
    {
        private readonly IAccountRepository accountRepository;
        private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;
        private readonly IIncomeTaxesRepository taxesRepository;

        private readonly IB3ResponseCalculatorService b3CalculatorService;
        private readonly IB3Client b3Client;

        private readonly ILogger<B3SyncingService> logger;

        public B3SyncingService(
            IAccountRepository accountRepository,
            IAverageTradedPriceRepostory averageTradedPriceRepository,
            IIncomeTaxesRepository taxesRepository,
            IB3ResponseCalculatorService b3CalculatorService,
            IB3Client b3Client,
            ILogger<B3SyncingService> logger
        )
        {
            this.accountRepository = accountRepository;
            this.averageTradedPriceRepository = averageTradedPriceRepository;
            this.taxesRepository = taxesRepository;
            this.b3CalculatorService = b3CalculatorService;
            this.b3Client = b3Client;
            this.logger = logger;
        }

        public async Task Sync(Guid accountId, List<BigBangRequest> request)
        {
            Infrastructure.Models.Account? account = accountRepository.GetById(accountId);
            if (account is null) throw new NotFoundException("Investidor", accountId.ToString());

            if (!AccountCanExecuteSyncing(account))
            {
                logger.LogInformation("O usuário {accountId} tentou executar a sincronização mas " +
                    "já sincronizou sua conta anteriormente.", account.Id);
                throw new BadRequestException($"O usuário {account.Id} tentou executar a sincronização mas " +
                    "já sincronizou sua conta anteriormente.");
            }

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            string startDate = "2019-11-01";

            string lastMonth = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1)
                .AddMonths(-1)
                .ToString("yyyy-MM-dd");
#pragma warning restore CS0219 // Variable is assigned but its value is never used

            // var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, lastMonth, accountId);
            var b3Response = GetBigBangMockedDataBeforeB3Contract();
            var response = await b3CalculatorService.Calculate(b3Response, accountId);

            if (response is null) return;

            await SaveB3Data(response, account);

            account.Status = EnumHelper.GetEnumDescription(AccountStatus.SubscriptionValid);
            accountRepository.Update(account);

            logger.LogInformation("Big Bang executado com sucesso para o usuário {accountId}.", accountId);
        }

        private static bool AccountCanExecuteSyncing(Infrastructure.Models.Account account)
        {
            return account.Status == EnumHelper.GetEnumDescription(AccountStatus.NeedToSync);
        }

        private static Models.B3.Movement.Root? GetBigBangMockedDataBeforeB3Contract()
        {
            Models.B3.Movement.Root? b3Response = new()
            {
                Data = new()
                {
                    EquitiesPeriods = new()
                    {
                        EquitiesMovements = new()
                    }
                }
            };

            AddBigBangDataSet(b3Response);

            return b3Response;
        }

        private async Task SaveB3Data(InvestorMovementDetails response, Infrastructure.Models.Account account)
        {
            List<IncomeTaxes> incomeTaxes = new();
            CreateIncomeTaxes(response.Assets, incomeTaxes, account);

            List<AverageTradedPrice> averageTradedPrices = new();
            CreateAverageTradedPrices(response.AverageTradedPrices, averageTradedPrices, account);

            // TODO unit of work and bulk insert. i swear to god i only did this because we're in a mvp
            foreach (var i in incomeTaxes)
            {
                await taxesRepository.AddAsync(i);
            }

            foreach (var a in averageTradedPrices)
            {
                await averageTradedPriceRepository.AddAsync(a);
            }
        }

        private static void CreateAverageTradedPrices(List<AverageTradedPriceDetails> response, List<AverageTradedPrice> averageTradedPrices, Infrastructure.Models.Account account)
        {
            foreach (var averageTradedPrice in response)
            {
                averageTradedPrices.Add(new AverageTradedPrice
                (
                   averageTradedPrice.TickerSymbol,
                   averageTradedPrice.AverageTradedPrice,
                   averageTradedPrice.TotalBought,
                   averageTradedPrice.TradedQuantity,
                   account,
                   updatedAt: DateTime.UtcNow
                ));
            }
        }

        private static void CreateIncomeTaxes(List<AssetIncomeTaxes> assets, List<Infrastructure.Models.IncomeTaxes> incomeTaxes, Infrastructure.Models.Account account)
        {
            foreach (var asset in assets)
            {
                if (MonthHadProfitOrLoss(asset))
                {
                    incomeTaxes.Add(new Infrastructure.Models.IncomeTaxes
                    (
                        asset.Month,
                        asset.Taxes,
                        asset.TotalSold,
                        asset.SwingTradeProfit,
                        asset.DayTradeProfit,
                        JsonConvert.SerializeObject(asset.TradedAssets),
                        account,
                        (int)asset.AssetTypeId
                    ));
                }
            }
        }

        private static bool MonthHadProfitOrLoss(AssetIncomeTaxes asset)
        {
            return asset.SwingTradeProfit != 0 || asset.DayTradeProfit != 0;
        }

        /// <summary>
        /// Retorna dados mockados da API da B3 para testes locais antes da contratação.
        /// Deve ser removido após a implementação do serviço de produção.
        /// </summary>
        private static void AddBigBangDataSet(Models.B3.Movement.Root response)
        {
            /**
                AverageTradedPrices

                BOVA11
                Total bought: 28,86
                Quantity: 2
                AVG: 14,43


                IVVB11
                Total bought: 492,3
                Quantity: 3
                AVG: 164,1


                AMER3
                Total bought: 527,08
                Quantity: 3
                AVG: 175,693333333

                IncomeTaxes

                01/2023

                Taxes: 21,9825
                Swing-trade profit: 144,55
                Day-trade profit: 0

                02/2023
                Taxes: 11,74734
                Swing-trade profit: 0
                Day-trade profit: 58,7367            
            */

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "BOVA11",
                CorporationName = "BOVA 11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 10.43,
                UnitPrice = 10.43,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 01)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "BOVA11",
                CorporationName = "BOVA 11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 18.43,
                UnitPrice = 18.43,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 03)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "BOVA11",
                CorporationName = "BOVA 11 Corporation Inc.",
                MovementType = "Venda",
                OperationValue = 12.54,
                UnitPrice = 12.54,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 08)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "IVVB11",
                CorporationName = "IVVB 11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 245.65,
                UnitPrice = 245.65,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 09)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "IVVB11",
                CorporationName = "IVVB 11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 246.65,
                UnitPrice = 123.325,
                EquitiesQuantity = 2,
                ReferenceDate = new DateTime(2023, 01, 09)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "IVVB11",
                CorporationName = "IVVB 11 Corporation Inc.",
                MovementType = "Venda",
                OperationValue = 304.54,
                UnitPrice = 304.54,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 10)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "FII - Fundo de Investimento Imobiliário",
                TickerSymbol = "KFOF11",
                CorporationName = "KFOF11 Corporation Inc.",
                MovementType = "Compra",
                OperationValue = 231.34,
                UnitPrice = 231.34,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 16)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "FII - Fundo de Investimento Imobiliário",
                TickerSymbol = "KFOF11",
                CorporationName = "KFOF11 Corporation Inc.",
                MovementType = "Venda",
                OperationValue = 237.34,
                UnitPrice = 237.34,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 28)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "AMER3",
                CorporationName = "Americanas S/A",
                MovementType = "Venda",
                OperationValue = 234.43,
                UnitPrice = 234.43,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 02, 01)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "AMER3",
                MovementType = "Compra",
                CorporationName = "Americanas S/A",
                OperationValue = 265.54,
                UnitPrice = 132.77,
                EquitiesQuantity = 2,
                ReferenceDate = new DateTime(2023, 02, 01)
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Models.B3.Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "AMER3",
                MovementType = "Compra",
                CorporationName = "Americanas S/A",
                OperationValue = 261.54,
                UnitPrice = 261.54,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 02, 01)
            });
        }
    }
}
