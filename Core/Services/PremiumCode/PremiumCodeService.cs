using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Repositories;

namespace Core.Services.PremiumCode
{
    public class PremiumCodeService : IPremiumCodeService
    {
        private readonly IGenericRepository<Infrastructure.Models.PremiumCode> genericRepository;

        public PremiumCodeService(IGenericRepository<Infrastructure.Models.PremiumCode> genericRepository)
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
