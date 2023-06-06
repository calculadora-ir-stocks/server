using Microsoft.Extensions.Logging;
using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks_core.Calculators;
using stocks_core.DTOs.B3;
using stocks_core.Models;
using stocks_core.Requests.BigBang;
using stocks_core.Services.BigBang;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_infrastructure.Repositories.IncomeTaxes;

namespace stocks.Services.IncomeTaxes;

public class IncomeTaxesService : IIncomeTaxesService
{
    private readonly IIncomeTaxesCalculator incomeTaxCalculator;

    private readonly IGenericRepository<Account> genericRepositoryAccount;
    private readonly IIncomeTaxesRepository incomeTaxesRepository;
    private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

    private readonly IB3Client client;

    private readonly ILogger<IncomeTaxesService> logger;

    public IncomeTaxesService(IIncomeTaxesCalculator incomeTaxCalculator,
        IGenericRepository<Account> genericRepositoryAccount,
        IIncomeTaxesRepository incomeTaxesRepository,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client client,
        ILogger<IncomeTaxesService> logger
        )
    {
        this.incomeTaxCalculator = incomeTaxCalculator;

        this.genericRepositoryAccount = genericRepositoryAccount;
        this.incomeTaxesRepository = incomeTaxesRepository;
        this.averageTradedPriceRepository = averageTradedPriceRepository;

        this.client = client;

        this.logger = logger;
    }

    #region Calcula todos os impostos de renda retroativos.
    public async Task BigBang(Guid accountId, List<BigBangRequest> request)
    {
        if (AlreadyHasAverageTradedPrice(accountId))
        {
            logger.LogInformation($"Big bang foi executado para o usuário {accountId}, mas ele já possui o preço médio e imposto de renda " +
                $"calculado na base.");
            return;
        }

        try
        {
            string minimumAllowedStartDateByB3 = "2019-11-01";
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            //Movement.Root? response = await client.GetAccountMovement("97188167044", minimumAllowedStartDateByB3, referenceEndDate: yesterday, accountId);            
            Movement.Root? response = new();
            response.Data = new();
            response.Data.EquitiesPeriods = new();
            response.Data.EquitiesPeriods.EquitiesMovements = new();

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "VALE3",
                CorporationName = "Vale S.A.",
                MovementType = "Compra",
                OperationValue = 21405,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 01),
                UnitPrice = 21405
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "VALE3",
                CorporationName = "Vale S.A.",
                MovementType = "Venda",
                OperationValue = 25000,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 02),
                UnitPrice = 25000
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "BOVA11",
                CorporationName = "BOVA",
                MovementType = "Compra",
                OperationValue = 439,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 03),
                UnitPrice = 439
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "ETF - Exchange Traded Fund",
                TickerSymbol = "BOVA11",
                CorporationName = "BOVA",
                MovementType = "Venda",
                OperationValue = 768,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 03),
                UnitPrice = 768,
                DayTraded = true
            });

            BigBang bigBang = new(incomeTaxCalculator);
            var taxesToBePaid = bigBang.Calculate(response);

            await SaveBigBang(taxesToBePaid, accountId);

            logger.LogInformation($"Big Bang executado com sucesso para o usuário {accountId}.");
        } catch (Exception e)
        {
            logger.LogError($"Uma exceção ocorreu ao executar o Big Bang do usuário {accountId}." +
                $"{e.Message}");

            throw;
        }
    }

    private bool AlreadyHasAverageTradedPrice(Guid accountId)
    {
        try
        {
            return averageTradedPriceRepository.AlreadyHasAverageTradedPrice(accountId);
        } catch (Exception e)
        {
            logger.LogError($"Uma exceção ocorreu ao tentar verificar se o usuário {accountId} já possuia o big bang calculado." +
                $"{e.Message}");
            throw;
        }
    }

    private async Task SaveBigBang(Dictionary<string, List<AssetIncomeTaxes>> response, Guid accountId)
    {
        Account account = genericRepositoryAccount.GetById(accountId);

        List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes = new();

        foreach (var month in response)
        {
            AddIncomeTaxes(month, incomeTaxes, account);
        }

        List<AverageTradedPrice> averageTradedPrices = new();
        AddAverageTradedPrices(response, averageTradedPrices, account);

        await incomeTaxesRepository.AddAllAsync(incomeTaxes);
        await averageTradedPriceRepository.AddAllAsync(averageTradedPrices);
    }

    private void AddAverageTradedPrices(Dictionary<string, List<AssetIncomeTaxes>> response, List<AverageTradedPrice> averageTradedPricesList, Account account)
    {
        //var averageTradedPrices = response.First().Value.First().AverageTradedPrices;

        //foreach (var averageTradedPrice in averageTradedPrices)
        //{
        //    averageTradedPricesList.Add(new AverageTradedPrice
        //    {
        //        Ticker = averageTradedPrice.Key,
        //        AveragePrice = averageTradedPrice.Value.AverageTradedPrice,
        //        Quantity = averageTradedPrice.Value.TradedQuantity,
        //        Account = account,
        //        UpdatedAt = DateTime.UtcNow
        //    });
        //}
    }

    private void AddIncomeTaxes(KeyValuePair<string, List<AssetIncomeTaxes>> month, List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes, Account account)
    {
        foreach (var asset in month.Value)
        {
            if (asset.Taxes > 0)
            {
                incomeTaxes.Add(new stocks_infrastructure.Models.IncomeTaxes
                {
                    Month = month.Key,
                    TotalTaxes = asset.Taxes,
                    TotalSold = asset.TotalSold,
                    SwingTradeProfit = asset.SwingTradeProfit,
                    DayTradeProfit = asset.DayTradeProfit,
                    TradedAssets = asset.TradedAssets,
                    Account = account,
                    AssetId = (int)asset.AssetTypeId
                });
            }
        }
    }

    #endregion

    #region Calcula o imposto de renda mensal.
    public Task<AssetIncomeTaxes?> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId)
    {
        throw new NotImplementedException();
    }
    #endregion
}
