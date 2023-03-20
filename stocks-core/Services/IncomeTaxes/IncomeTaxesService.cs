using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks_core.Business;
using stocks_core.DTOs.B3;
using stocks_core.Enums;
using stocks_core.Response;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks.Services.IncomeTaxes;

public class IncomeTaxesService : IIncomeTaxesService
{
    private readonly IGenericRepository<Account> _genericRepositoryAccount;
    private static IAverageTradedPriceRepostory _averageTradedPriceRepository;

    private readonly IB3Client _client;

    private IIncomeTaxesCalculation _incomeTaxCalculator;

    private const string SellOperation = "Transfer�ncia";

    public IncomeTaxesService(IGenericRepository<Account> genericRepositoryAccount,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client b3Client, IIncomeTaxesCalculation calculator)
    {
        _genericRepositoryAccount = genericRepositoryAccount;
        _averageTradedPriceRepository = averageTradedPriceRepository;
        _client = b3Client;
        _incomeTaxCalculator = calculator;
    }

    public async Task<CalculateAssetsIncomeTaxesResponse?> CalculateAssetsIncomeTaxes(Guid accountId)
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
                MovementType = "Venda",
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

            CalculateAssetsIncomeTaxesResponse? response = null;

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

    private async Task AddAllRequiredIncomeTaxesToObject(Movement.Root httpClientResponse, CalculateAssetsIncomeTaxesResponse? response, Guid accountId)
    {
        var movements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;

        var stocks = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Stocks));
        var etfs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.ETFs));
        var fiis = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FIIs));
        var bdrs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.BDRs));
        var gold = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Gold));
        var fundInvestments = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FundInvestments));

        _incomeTaxCalculator = new StocksIncomeTaxes(_averageTradedPriceRepository);
        await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, stocks, accountId);

        _incomeTaxCalculator = new ETFsIncomeTaxes();
        await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, etfs, accountId);

        // _incomeTaxCalculator = new FIIsIncomeTaxes();
        await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, fiis, accountId);

        // _incomeTaxCalculator = new BDRsIncomeTaxes();
        await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, bdrs, accountId);

        // _incomeTaxCalculator = new GoldIncomeTaxes();
        await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, gold, accountId);

        // _incomeTaxCalculator = new FundInvestmentsIncomeTaxes();
        await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, fundInvestments, accountId);
    }

    private static bool InvestorSoldAnyAsset(Movement.Root httpClientResponse)
    {
        var allMovements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;

        var sellOperationMovements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Where(
            asset => asset.MovementType.Equals(SellOperation)).FirstOrDefault();

        bool investorSoldAnyAsset = allMovements.Contains(sellOperationMovements!);

        // Mockado por enquanto porque a B3 nao possui movimentos de venda em ambiente
        // de certificacao. 
        return true;
    }

    public class Asset
    {
        public string Ticker { get; set; }
        public int TradeQuantity { get; set; }
        public double GrossAmount { get; set; }
        public DateTime TradeDateTime { get; set; }

        public double Total { get; set; }
    }
}
