using Microsoft.Extensions.Logging;
using Infrastructure.Repositories.Account;
using Common.Constants;

namespace Core.Services.Hangfire.UserPlansValidity
{
    public class UserPlansValidityHangfire : IUserPlansValidityHangfire
    {
        private readonly IAccountRepository accountRepository;
        private readonly ILogger<UserPlansValidityHangfire> logger;

        /// <summary>
        /// Quando um usuário é cadastrado no pré-lançamento, ele tem direto a 3 meses gratuitos de serviço.
        /// </summary>
        private const int FreePremiumPlan = 3;

        private const int FreePlan = 1;
        private const int SemesterPlan = 6;
        private const int AnualPlan = 12;

        public UserPlansValidityHangfire(IAccountRepository accountRepository, ILogger<UserPlansValidityHangfire> logger)
        {
            this.accountRepository = accountRepository;
            this.logger = logger;
        }

        public void UpdateUsersPlanExpiration()
        {
            try
            {
                logger.LogInformation("Iniciando Hangfire para reavaliar planos expirados.");

                var users = accountRepository.GetAll();

                int freePremiumCount = 0;
                int freeCount = 0;
                int semesterCount = 0;
                int anualCount = 0;

                foreach (var user in users)
                {
                    switch (user.PlanId)
                    {
                        case PlansConstants.Monthly:
                        case PlansConstants.Free:

                            if (user.IsPremium && IsFreePremiumPlanExpired(user))
                            {
                                user.IsPlanExpired = true;
                                freePremiumCount += 1;
                            }

                            if (!user.IsPremium)
                            {
                                if (IsFreePlanExpired(user))
                                {
                                    user.IsPlanExpired = true;
                                    freeCount += 1;
                                }
                            }

                            break;
                        case PlansConstants.Semester:
                            if (IsSemesterPlanExpired(user))
                            {
                                user.IsPlanExpired = true;
                                semesterCount += 1;
                            }
                            break;
                        case PlansConstants.Anual:
                            if (IsAnualPlanExpired(user))
                            {
                                user.IsPlanExpired = true;
                                anualCount += 1;
                            }
                            break;
                    }
                }

                logger.LogInformation("Finalizando Hangfire para reavaliar planos expirados. " +
                    "{0} planos premiums expirados, {1} planos gratuitos expirados, {2} planos semestrais expirados e {3} planos anuais expirados. ", 
                    freePremiumCount,
                    freeCount,
                    semesterCount,
                    anualCount
                );
            }
            catch (Exception e)
            {
                logger.LogError("Ocorreu um erro ao invalidar o plano de 3 meses de usuários premiums. {e}", e.Message);
            }
        }

        private static bool IsAnualPlanExpired(Infrastructure.Models.Account user)
        {
            return DateTime.Now.AddMonths(-AnualPlan) > user.PlanStartDate;
        }

        private static bool IsSemesterPlanExpired(Infrastructure.Models.Account user)
        {
            return DateTime.Now.AddMonths(-SemesterPlan) > user.PlanStartDate;
        }

        private static bool IsFreePlanExpired(Infrastructure.Models.Account user)
        {
            return DateTime.Now.AddMonths(-FreePlan) > user.PlanStartDate;
        }

        private static bool IsFreePremiumPlanExpired(Infrastructure.Models.Account user)
        {
            return DateTime.Now.AddMonths(-FreePremiumPlan) > user.PlanStartDate;
        }
    }
}
