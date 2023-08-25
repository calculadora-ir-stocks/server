using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using stocks.Repositories;

namespace stocks_core.Services.PremiumCode
{
    public class PremiumCodeService : IPremiumCodeService
    {
        private readonly IGenericRepository<stocks_infrastructure.Models.PremiumCode> genericRepository;

        public PremiumCodeService(IGenericRepository<stocks_infrastructure.Models.PremiumCode> genericRepository)
        {
            this.genericRepository = genericRepository;
        }

        public bool IsValid(string code)
        {
            var premiumCode = genericRepository.GetAll().Where(x => x.Code.Equals(code));
            return !premiumCode.IsNullOrEmpty();
        }

        public void DeactivatePremiumCode(string code)
        {
            var premiumCode = genericRepository.GetAll().Where(x => x.Code.Equals(code)).SingleOrDefault();
            if (premiumCode is null) return;

            premiumCode.IsActive = false;
            genericRepository.Update(premiumCode!);
        }

        public bool Active(string code)
        {
            var premiumCode = genericRepository.GetAll().Where(x => x.Code.Equals(code)).SingleOrDefault();
            if (premiumCode is null) return false;

            return premiumCode.IsActive;
        }
    }
}
