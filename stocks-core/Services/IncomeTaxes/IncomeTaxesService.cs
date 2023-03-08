using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks_core.Business;
using stocks_core.DTOs.B3;
using stocks_core.Enums;
using stocks_core.Response;

namespace stocks.Services.IncomeTaxes;

public class IncomeTaxesService : IIncomeTaxesService
{
    private readonly IGenericRepository<Account> _genericRepositoryAccount;
    private readonly IB3Client _client;

    private IIncomeTaxesCalculation _incomeTaxCalculator;

    private const string SellOperation = "Transfer�ncia";
    private const string BuyOperation = "Compra";

    public IncomeTaxesService(IGenericRepository<Account> genericRepositoryAccount, IB3Client b3Client, IIncomeTaxesCalculation calculator)
    {
        _genericRepositoryAccount = genericRepositoryAccount;
        _client = b3Client;
        _incomeTaxCalculator = calculator;
    }

    public async Task<CalculateAssetsIncomeTaxesResponse?> CalculateAssetsIncomeTaxes(Guid accountId, string? referenceStartDate, string? referenceEndDate)
    {
        string mockedCpf = "97188167044";

        // A consulta da B3 apenas funciona em D-1, ou seja, as consultas sempre s�o feitas com base
        // no dia anterior.
        referenceStartDate ??= DateTime.Now.ToString("yyyy-MM-01");
        referenceEndDate ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        try
        {
            Movement.Root? httpClientResponse = await _client.GetAccountMovement(mockedCpf, "2022-01-01", "2022-02-20")!;

            CalculateAssetsIncomeTaxesResponse? response = null;

            if (InvestorSoldAnyAsset(httpClientResponse))
            {
                await AddAllRequiredIncomeTaxesToObject(httpClientResponse, response);
            }

            return response;

        } catch (Exception _)
        {
            throw;
        }
    }

    private async Task AddAllRequiredIncomeTaxesToObject(Movement.Root httpClientResponse, CalculateAssetsIncomeTaxesResponse? response)
    {
        var movements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;

        foreach (var movement in movements.Where(movement => movement.AssetType.Equals("ETF - Exchange Traded Fund")))
        {
            switch (movement.AssetType)
            {
                case AssetMovementTypes.Stocks:
                    _incomeTaxCalculator = new StocksIncomeTaxes();
                    await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, movement);
                    break;
                case AssetMovementTypes.ETFs:
                    _incomeTaxCalculator = new ETFsIncomeTaxes();
                    await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, movement);
                    break;
                case AssetMovementTypes.Gold:
                    //_incomeTaxCalculator = new GoldIncomeTaxes();
                    await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, movement);
                    break;
                case AssetMovementTypes.FundInvestments:
                    //_incomeTaxCalculator = new FundInvestmentsIncomeTaxes();
                    await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, movement);
                    break;
                case AssetMovementTypes.FIIs:
                    //_incomeTaxCalculator = new FIIsIncomeTaxes();
                    await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, movement);
                    break;
                case AssetMovementTypes.BDRs:
                    //_incomeTaxCalculator = new StocksIncomeTaxes();
                    await _incomeTaxCalculator.AddAllIncomeTaxesToObject(response, movement);
                    break;
            }
        }
        if (InvestorSoldAnyStock(httpClientResponse))
        {
            
        }
    }

    private static bool InvestorSoldAnyStock(Movement.Root httpClientResponse)
    {
        var allMovements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;
        var soldStock = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Where(
            asset => asset.MovementType.Equals(SellOperation) && asset.AssetType.Equals(AssetMovementTypes.Stocks)).FirstOrDefault();

        var investorSoldAnyStock = allMovements.Contains(soldStock!);

        return investorSoldAnyStock;
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
