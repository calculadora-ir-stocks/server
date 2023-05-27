using Microsoft.Extensions.Logging;
using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks.Requests;
using stocks_core.Business;
using stocks_core.Calculators.Assets;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_infrastructure.Repositories.IncomeTaxes;

namespace stocks.Services.IncomeTaxes;

public class IncomeTaxesService : IIncomeTaxesService
{
    private IIncomeTaxesCalculator incomeTaxCalculator;

    private readonly IAverageTradedPriceService averageTradedPriceService;

    private readonly IGenericRepository<Account> genericRepositoryAccount;
    private readonly IIncomeTaxesRepository incomeTaxesRepository;
    private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

    private readonly IB3Client client;

    private readonly ILogger<IncomeTaxesService> logger;

    public IncomeTaxesService(IIncomeTaxesCalculator incomeTaxCalculator,
        IAverageTradedPriceService averageTradedPriceService,        
        IGenericRepository<Account> genericRepositoryAccount,
        IIncomeTaxesRepository incomeTaxesRepository,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client client,
        ILogger<IncomeTaxesService> logger
        )
    {
        this.incomeTaxCalculator = incomeTaxCalculator;

        this.averageTradedPriceService = averageTradedPriceService;

        this.genericRepositoryAccount = genericRepositoryAccount;
        this.incomeTaxesRepository = incomeTaxesRepository;
        this.averageTradedPriceRepository = averageTradedPriceRepository;

        this.client = client;

        this.logger = logger;
    }

    #region Calcula o imposto de renda a ser pago no mês atual
    public async Task<AssetIncomeTaxes?> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId)
    {
        string mockedCpf = "97188167044";

        // A consulta da B3 apenas funciona em D-1, ou seja, as consultas sempre são feitas com base
        // no dia anterior.
        var referenceStartDate = DateTime.Now.ToString("yyyy-MM-01");
        var referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        try
        {
            // Movement.Root? httpClientResponse = await _client.GetAccountMovement(mockedCpf, "2022-01-01", "2022-02-20")!;
            Movement.Root? httpClientResponse = new();
            httpClientResponse.Data = new();
            httpClientResponse.Data.EquitiesPeriods = new();
            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements = new();

            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                MovementType = "Compra",
                OperationValue = 10.43,
                EquitiesQuantity = 2,
            });

            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                MovementType = "Venda",
                OperationValue = 13.12,
                EquitiesQuantity = 5,
            });

            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                MovementType = "Venda",
                OperationValue = 9.32,
                EquitiesQuantity = 3,
            });

            AssetIncomeTaxes? response = null;

            if (InvestorSoldAnyAsset(httpClientResponse))
            {
                await AddAllRequiredIncomeTaxesToObject(httpClientResponse, response, accountId);
            }

            return response;

        } catch (Exception _)
        {
            throw;
        }
    }

    private async Task AddAllRequiredIncomeTaxesToObject(Movement.Root httpClientResponse, AssetIncomeTaxes? response, Guid accountId)
    {
        var movements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;

        var stocks = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Stocks));
        var etfs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.ETFs));
        var fiis = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FIIs));
        var bdrs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.BDRs));
        var gold = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Gold));
        var fundInvestments = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FundInvestments));

        incomeTaxCalculator = new StocksIncomeTaxes();
        incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, stocks, accountId);

        incomeTaxCalculator = new ETFsIncomeTaxes();
        incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, etfs, accountId);

        // _incomeTaxCalculator = new FIIsIncomeTaxes();
        incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, fiis, accountId);

        // _incomeTaxCalculator = new BDRsIncomeTaxes();
        incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, bdrs, accountId);

        // _incomeTaxCalculator = new GoldIncomeTaxes();
        incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, gold, accountId);

        // _incomeTaxCalculator = new FundInvestmentsIncomeTaxes();
        incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, fundInvestments, accountId);
    }

    private static bool InvestorSoldAnyAsset(Movement.Root httpClientResponse)
    {
        var allMovements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;

        var sellOperationMovements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Where(
            asset => asset.MovementType.Equals(B3ServicesConstants.Sell)).FirstOrDefault();

        bool investorSoldAnyAsset = allMovements.Contains(sellOperationMovements!);

        // Mockado por enquanto porque a B3 não possui movimentos de venda em ambiente
        // de certificação.
        return true;
    }
    #endregion

    #region Calcula o imposto de renda a ser pago de 01/11/2019 até D-1.
    public async Task BigBang(Guid accountId, List<CalculateIncomeTaxesForEveryMonthRequest> request)
    {
        if (AccountAlreadyHasAverageTradedPrice(accountId))
        {
            logger.LogInformation($"Big bang foi executado para o usuário {accountId}, mas ele já possui o preço médio calculado na base.");
            return;
        }

        string minimumAllowedStartDateByB3 = "2019-11-01";
        string referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        try
        {
            // Movement.Root? response = await client.GetAccountMovement("97188167044", minimumAllowedStartDateByB3, referenceEndDate, accountId)!;
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
                OperationValue = 50000,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 10),
                UnitPrice = 50000
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "VALE3",
                CorporationName = "Vale S.A.",
                MovementType = "Compra",
                OperationValue = 43000,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 02, 10),
                UnitPrice = 43000
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "VALE3",
                CorporationName = "Vale S.A.",
                MovementType = "Venda",
                OperationValue = 48000,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 02, 11),
                UnitPrice = 48000
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "VALE3",
                CorporationName = "Vale S.A.",
                MovementType = "Compra",
                OperationValue = 49000,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 03, 12),
                UnitPrice = 49000
            });

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "VALE3",
                CorporationName = "Vale S.A.",
                MovementType = "Venda",
                OperationValue = 52000,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 03, 12),
                UnitPrice = 52000
            });

            BigBang bigBang = new(incomeTaxesRepository, averageTradedPriceRepository, genericRepositoryAccount);
            await bigBang.Calculate(response, incomeTaxCalculator, accountId);

            logger.LogInformation($"Big Bang executado com sucesso para o usuário {accountId}.");
        } catch (Exception e)
        {
            logger.LogError($"Uma exceção ocorreu ao executar o Big Bang do usuário {accountId}." +
                $"{e.Message}");
            throw;
        }
    }

    private bool AccountAlreadyHasAverageTradedPrice(Guid accountId)
    {
        try
        {
            return averageTradedPriceRepository.AccountAlreadyHasAverageTradedPrice(accountId);
        } catch (Exception e)
        {
            logger.LogError($"Uma exceção ocorreu ao tentar verificar se o usuário {accountId} já possuia o big bang calculado." +
                $"{e.Message}");
            throw;
        }
    }

    #endregion
}
