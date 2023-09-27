using Common.Enums;
using Common.Helpers;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.Plan;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Core.Hangfire.PlanExpirer
{
    public class PlanExpirerHangfire : IPlanExpirerHangfire
    {
        private readonly IAccountRepository accountRepository;
        private readonly IPlanRepository planRepository;

        private readonly ILogger<PlanExpirerHangfire> logger;

        public PlanExpirerHangfire(IAccountRepository accountRepository, IPlanRepository planRepository, ILogger<PlanExpirerHangfire> logger)
        {
            this.accountRepository = accountRepository;
            this.planRepository = planRepository;
            this.logger = logger;
        }

        public void Execute()
        {
            try
            {
                Guid threadId = new();

                logger.LogInformation("Iniciando Hangfire para atualizar planos expirados de investidores." +
                    "Id do processo: {id}", threadId);

                Stopwatch timer = new();
                timer.Start();

                var plans = planRepository.GetAllAccountPlans();
                int expiredPlans = 0;

                foreach (var plan in plans)
                {
                    if (plan.ExpiresAt <= DateTime.Now)
                    {
                        var account = accountRepository.GetById(plan.AccountId)!;
                        account.Status = EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired);

                        accountRepository.Update(account);
                    }
                }

                timer.Stop();
                var timeTakenForEach = timer.Elapsed;

                logger.LogInformation("Finalizando Hangfire para reavaliar planos expirados. " +
                    "{expiredPlans} planos foram expirados de um total de {total} planos.", expiredPlans, plans.Count());
            }
            catch (Exception e)
            {
                logger.LogError("Ocorreu um erro ao invalidar o plano de 3 meses de usuários premiums. {e}", e.Message);
            }
        }
    }
}
