using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using stocks.Repositories;

namespace stocks_core.Services.PremiumCode
{
    public class PremiumCodeService : IPremiumCodeService
    {
        private readonly IGenericRepository<stocks_infrastructure.Models.PremiumCode> genericRepository;
        private readonly ILogger<PremiumCodeService> logger;

        public PremiumCodeService(IGenericRepository<stocks_infrastructure.Models.PremiumCode> genericRepository, ILogger<PremiumCodeService> logger)
        {
            this.genericRepository = genericRepository;
            this.logger = logger;
        }

        public bool IsValid(string code)
        {
            var premiumCode = genericRepository.GetAll().Where(x => x.Code.Equals(code));
            return !premiumCode.IsNullOrEmpty();
        }
    }
}
