using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using stocks_core.Models.Responses;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks_core.Services.WalletService;

public class WalletService : IWalletService
{
    private readonly IAverageTradedPriceRepostory averageTradedPriceRepostory;
    private readonly ILogger<WalletService> logger;

    public WalletService(IAverageTradedPriceRepostory averageTradedPriceRepostory, ILogger<WalletService> logger)
    {
        this.averageTradedPriceRepostory = averageTradedPriceRepostory;
        this.logger = logger;
    }

    public IEnumerable<GetAllAssetsResponse> GetAllAssets(Guid accountId)
    {
        try
        {
            var assets = averageTradedPriceRepostory.GetAverageTradedPrices(accountId);

            if (assets is null) return Array.Empty<GetAllAssetsResponse>();

            return ToDto(assets);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ocorreu um erro ao executar {name}: {error}", nameof(GetAllAssets), e.Message);
            throw;
        }
    }

    private static IEnumerable<GetAllAssetsResponse> ToDto(List<AverageTradedPrice> assets)
    {
        foreach (var item in assets)
        {
            yield return new GetAllAssetsResponse(item.Ticker, item.AveragePrice, item.Quantity);
        }
    }
}
