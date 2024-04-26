using Bogus;
using Common.Constants;
using Common.Enums;
using Common.Helpers;
using Core.Hangfire.PlanExpirer;
using Infrastructure.Models;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.Plan;
using Microsoft.Extensions.Logging;
using Moq;

namespace stocks_unit_tests.Hangfire
{
    public class PlanExpirerHangfireTest
    {
        private readonly Mock<IAccountRepository> accountRepository;
        private readonly Mock<IPlanRepository> planRepository;

        private readonly Mock<ILogger<PlanExpirerHangfire>> logger;
        private readonly PlanExpirerHangfire service;

        public PlanExpirerHangfireTest()
        {
            accountRepository = new Mock<IAccountRepository>();
            planRepository = new Mock<IPlanRepository>();

            logger = new Mock<ILogger<PlanExpirerHangfire>>();

            service = new PlanExpirerHangfire(accountRepository.Object, planRepository.Object, logger.Object);
        }

        [Fact(DisplayName = "Deve invalidar planos gratuitos, planos mensais, planos semestrais e planos anuais expirados.")]
        public async Task Should_invalidate_users_with_expired_plans()
        {
            var users = new Faker<Account>().Generate(10);

            List<Plan> plans = new()
            {
                new Plan(PlansConstants.Free, users[0].Id, users[0], DateTime.Now), //expired
                new Plan(PlansConstants.Semester, users[1].Id, users[1], DateTime.Now.AddDays(1)),
                new Plan(PlansConstants.Free, users[2].Id, users[2], DateTime.Now.AddMonths(2)),
                new Plan(PlansConstants.Anual, users[3].Id, users[3], DateTime.Now.AddDays(-1)), //expired
                new Plan(PlansConstants.Free, users[4].Id, users[4], DateTime.Now.AddMinutes(-1)), //expired
                new Plan(PlansConstants.Semester, users[5].Id, users[5], DateTime.Now.AddYears(1)),
                new Plan(PlansConstants.Free, users[6].Id, users[6], DateTime.Now.AddDays(6)),
                new Plan(PlansConstants.Anual, users[7].Id, users[7], DateTime.Now.AddDays(1)),
                new Plan(PlansConstants.Anual, users[8].Id, users[8], DateTime.Now.AddDays(-4)), //expired
                new Plan(PlansConstants.Free, users[9].Id, users[9], DateTime.Now.AddDays(1)),
            };

            planRepository.Setup(x => x.GetAllAccountPlans()).Returns(plans);

            accountRepository.Setup(x => x.GetById(users[0].Id)).ReturnsAsync(users[0]);
            accountRepository.Setup(x => x.GetById(users[3].Id)).ReturnsAsync(users[3]);
            accountRepository.Setup(x => x.GetById(users[4].Id)).ReturnsAsync(users[4]);
            accountRepository.Setup(x => x.GetById(users[8].Id)).ReturnsAsync(users[8]);

            await service.Execute();

            int expiredPlans = users.Where(x => x.Status == EnumHelper.GetEnumDescription(AccountStatus.SubscriptionExpired)).Count();

            Assert.Equal(4, expiredPlans);
        }
    }
}
