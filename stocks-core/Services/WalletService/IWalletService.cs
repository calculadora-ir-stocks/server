using stocks_core.Models.Responses;

namespace stocks_core.Services.WalletService;

public interface IWalletService
{
    IEnumerable<GetAllAssetsResponse> GetAllAssets(Guid accountId);
}
