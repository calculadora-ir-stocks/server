using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using stocks.Clients.B3;
using stocks.Exceptions;
using stocks.Repositories;
using stocks_common.Models;
using stocks_core.Models;
using stocks_core.Requests.BigBang;
using stocks_core.Responses;
using stocks_core.Services.IncomeTaxes;
using stocks_infrastructure.Dtos;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_infrastructure.Repositories.IncomeTaxes;

namespace stocks.Services.IncomeTaxes;

public class AssetsService : IAssetsService
{
    private readonly IIncomeTaxesService incomeTaxesService;

    private readonly IGenericRepository<Account> genericRepositoryAccount;
    private readonly IIncomeTaxesRepository incomeTaxesRepository;
    private readonly IAverageTradedPriceRepostory averageTradedPriceRepository;

    private readonly IB3Client b3Client;

    private readonly ILogger<AssetsService> logger;

    public AssetsService(IIncomeTaxesService incomeTaxesService,
        IGenericRepository<Account> genericRepositoryAccount,
        IIncomeTaxesRepository incomeTaxesRepository,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client b3Client,
        ILogger<AssetsService> logger
        )
    {
        this.incomeTaxesService = incomeTaxesService;
        this.genericRepositoryAccount = genericRepositoryAccount;
        this.incomeTaxesRepository = incomeTaxesRepository;
        this.averageTradedPriceRepository = averageTradedPriceRepository;
        this.b3Client = b3Client;
        this.logger = logger;
    }

    #region Calcula todos os impostos de renda retroativos.
    public async Task BigBang(Guid accountId, List<BigBangRequest> request)
    {
        try
        {
            if (AlreadyHasAverageTradedPrice(accountId))
            {
                logger.LogInformation("Big bang foi executado para o usuário {accountId}, mas ele já possui o preço médio e imposto de renda " +
                    "calculado na base.", accountId);

                throw new InvalidBusinessRuleException("O preço médio e o imposto de renda para esse usuário já foram calculados.");
            }

            Account account = await genericRepositoryAccount.GetByIdAsync(accountId);

            // A B3 apenas possui dados a partir de 01/11/2019.
            string startDate = "2019-11-01";

            string lastMonth = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: 1)
                .AddMonths(-1)
                .ToString("yyyy-MM-dd");

            var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, lastMonth, accountId);

            var taxesAndAverageTradedPrices = await incomeTaxesService.Execute(b3Response, accountId);

            await SaveIncomeTaxes(taxesAndAverageTradedPrices, accountId);

            logger.LogInformation("Big Bang executado com sucesso para o usuário {accountId}.", accountId);
        }
        catch (Exception e)
        {
            logger.LogError("Uma exceção ocorreu ao executar o Big Bang do usuário {accountId}." +
                "{e.Message}", accountId, e.Message);

            throw;
        }
    }

    private bool AlreadyHasAverageTradedPrice(Guid accountId) =>
        averageTradedPriceRepository.AlreadyHasAverageTradedPrice(accountId);

    private async Task SaveIncomeTaxes((List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>) response, Guid accountId)
    {
        Account account = genericRepositoryAccount.GetById(accountId);

        List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes = new();
        CreateIncomeTaxes(response.Item1, incomeTaxes, account);

        List<AverageTradedPrice> averageTradedPrices = new();
        CreateAverageTradedPrices(response.Item2, averageTradedPrices, account);

        // TO-DO: unit of work
        await incomeTaxesRepository.AddAllAsync(incomeTaxes);
        await averageTradedPriceRepository.AddAllAsync(averageTradedPrices);
    }

    private void CreateAverageTradedPrices(List<AverageTradedPriceDetails> response, List<AverageTradedPrice> averageTradedPrices, Account account)
    {
        foreach (var averageTradedPrice in response)
        {
            averageTradedPrices.Add(new AverageTradedPrice
            (
               averageTradedPrice.TickerSymbol,
               averageTradedPrice.AverageTradedPrice,
               averageTradedPrice.TradedQuantity,
               account,
               updatedAt: DateTime.UtcNow
            ));
        }
    }

    private void CreateIncomeTaxes(List<AssetIncomeTaxes> assets, List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes, Account account)
    {
        foreach (var asset in assets)
        {
            if (MovementHadProfitOrLoss(asset))
            {
                incomeTaxes.Add(new stocks_infrastructure.Models.IncomeTaxes
                {
                    Month = asset.Month,
                    TotalTaxes = asset.Taxes,
                    TotalSold = asset.TotalSold,
                    SwingTradeProfit = asset.SwingTradeProfit,
                    DayTradeProfit = asset.DayTradeProfit,
                    TradedAssets = JsonConvert.SerializeObject(asset.TradedAssets),
                    Account = account,
                    AssetId = (int)asset.AssetTypeId
                });
            }
        }
    }

    /// <summary>
    /// Define se um determinado mês houveram prejuízos ou lucros em operações swing-trade ou day-trade.
    /// </summary>
    private bool MovementHadProfitOrLoss(AssetIncomeTaxes asset)
    {
        return asset.SwingTradeProfit != 0 || asset.DayTradeProfit != 0;
    }

    #endregion

    #region Calcula o imposto de renda do mês atual.
    public async Task<MonthTaxesResponse> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId)
    {
        try
        {
            // Caso seja dia 1, não há como obter os dados do mês atual já que a B3 disponibiliza os dados em D-1.
            if (IsDayOne())
            {
                // Porém, sendo dia 1, o Worker já salvou os dados do mês passado na base.
                return await CalculateSpecifiedMonthAssetsIncomeTaxes(DateTime.Now.ToString("yyyy-MM"), accountId);
            }

            string startDate = DateTime.Now.ToString("yyyy-MM-01");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            Account account = await genericRepositoryAccount.GetByIdAsync(accountId);

            var b3Response = await b3Client.GetAccountMovement(account.CPF, startDate, yesterday, account.Id);

            var response = await incomeTaxesService.Execute(b3Response, account.Id);

            return CurrentMonthToDto(response.Item1);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao calcular o imposto mensal devido. {e.Message}", e.Message);
            throw;
        }
    }

    private static bool IsDayOne()
    {
        // D-1
        DateTime yesterday = DateTime.Now.AddDays(-1);
        return yesterday.Month < DateTime.Now.Month;
    }

    private MonthTaxesResponse CurrentMonthToDto(List<AssetIncomeTaxes> item1)
    {
        double totalTaxes = item1.Select(x => x.Taxes).Sum();
        List<stocks_core.Responses.Asset> tradedAssets = new();

        foreach (var item in item1)
        {
            tradedAssets.Add(new stocks_core.Responses.Asset(
                item.AssetTypeId,
                item.AssetName,
                item.Taxes,
                item.TotalSold,
                item.SwingTradeProfit,
                item.DayTradeProfit,
                item.TradedAssets
            ));
        }

        return new MonthTaxesResponse(
            taxes: totalTaxes,
            tradedAssets
        );
    }
    #endregion

    #region Calcula o imposto de renda do mês especificado.
    public async Task<MonthTaxesResponse> CalculateSpecifiedMonthAssetsIncomeTaxes(string month, Guid accountId)
    {
        try
        {
            if (WorkerDidNotSaveDataForThisMonthYet(month))
            {
                throw new InvalidBusinessRuleException("Para obter as informações de impostos do mês atual, acesse /assets/current.");
            }

            var response = await incomeTaxesRepository.GetSpecifiedMonthAssetsIncomeTaxes(System.Net.WebUtility.UrlDecode(month), accountId);

            return SpecifiedMonthToDto(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Ocorreu um erro ao calcular um imposto mensal devido especificado. {e.Message}");
            throw;
        }
    }

    private MonthTaxesResponse SpecifiedMonthToDto(IEnumerable<SpecifiedMonthAssetsIncomeTaxesDto> taxes)
    {
        double totalMonthTaxes = taxes.Select(x => x.Taxes).Sum();
        List<stocks_core.Responses.Asset> tradedAssets = new();

        foreach (var tax in taxes)
        {
            tradedAssets.Add(new stocks_core.Responses.Asset(
                (stocks_common.Enums.Asset)tax.AssetTypeId,
                tax.AssetName,
                tax.Taxes,
                tax.TotalSold,
                tax.SwingTradeProfit,
                tax.DayTradeProfit,
                tax.TradedAssets
            ));
        }

        return new MonthTaxesResponse(
            totalMonthTaxes,
            tradedAssets
        );
    }

    private bool WorkerDidNotSaveDataForThisMonthYet(string month)
    {
        string currentMonth = DateTime.Now.ToString("yyyy-MM");
        return month == currentMonth;
    }
    #endregion
}
