using Microsoft.Extensions.Logging;
using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks_common.Models;
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

    private readonly IB3Client b3Client;

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

        this.b3Client = client;

        this.logger = logger;
    }

    #region Calcula todos os impostos de renda retroativos.
    public async Task BigBang(Guid accountId, List<BigBangRequest> request)
    {        
        try
        {
            if (AlreadyHasAverageTradedPrice(accountId))
            {
                logger.LogInformation($"Big bang foi executado para o usuário {accountId}, mas ele já possui o preço médio e imposto de renda " +
                    $"calculado na base.");
                return;
            }

            string minimumAllowedStartDate = "2019-11-01";
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            //TODO: remove mock and get user CPF.
            Movement.Root? b3Response = await b3Client.GetAccountMovement("97188167044", minimumAllowedStartDate, referenceEndDate: yesterday, accountId);            

            BigBang bigBang = new(incomeTaxCalculator);
            var bigBangResponse = bigBang.Calculate(b3Response);

            await SaveBigBang(bigBangResponse, accountId);

            logger.LogInformation($"Big Bang executado com sucesso para o usuário {accountId}.");
        } catch (Exception e)
        {
            logger.LogError($"Uma exceção ocorreu ao executar o Big Bang do usuário {accountId}." +
                $"{e.Message}");

            throw;
        }
    } 

    private bool AlreadyHasAverageTradedPrice(Guid accountId) =>
        averageTradedPriceRepository.AlreadyHasAverageTradedPrice(accountId);

    private async Task SaveBigBang((List<AssetIncomeTaxes>, List<AverageTradedPriceDetails>) response, Guid accountId)
    {
        Account account = genericRepositoryAccount.GetById(accountId);

        List<stocks_infrastructure.Models.IncomeTaxes> incomeTaxes = new();
        CreateIncomeTaxes(response.Item1, incomeTaxes, account);

        List<AverageTradedPrice> averageTradedPrices = new();
        CreateAverageTradedPrices(response.Item2, averageTradedPrices, account);

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
                    TradedAssets = asset.TradedAssets,
                    Account = account,
                    AssetId = (int)asset.AssetTypeId
                });
            }
        }
    }

    /// <summary>
    /// Salva na base apenas 
    /// </summary>
    private bool MovementHadProfitOrLoss(AssetIncomeTaxes asset)
    {
        return asset.SwingTradeProfit != 0 || asset.DayTradeProfit != 0;
    }

    #endregion

    #region Calcula o imposto de renda mensal.
    public Task<AssetIncomeTaxes?> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId)
    {
        throw new NotImplementedException();
    }
    #endregion
}
