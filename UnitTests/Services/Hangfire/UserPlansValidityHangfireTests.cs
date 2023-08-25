using Bogus;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Infrastructure.Repositories.Account;
using Common.Constants;
using Core.Services.Hangfire.UserPlansValidity;
using Infrastructure.Models;

namespace stocks_unit_tests.Services.Hangfire
{
    public class UserPlansValidityHangfireTests
    {
        private readonly Mock<IAccountRepository> accountRepository;
        private readonly Mock<ILogger<UserPlansValidityHangfire>> logger;
        private readonly IUserPlansValidityHangfire service;

        public UserPlansValidityHangfireTests()
        {
            this.accountRepository = new Mock<IAccountRepository>();
            this.logger = new Mock<ILogger<UserPlansValidityHangfire>>();

            service = new UserPlansValidityHangfire(accountRepository.Object, logger.Object);
        }

        [Fact(DisplayName = "Usuários que se cadastraram no pré-lançamento são usuários premium e possuem 3 meses gratuitos. Após os 3 meses terem sido" +
            "utilizados, o job Hangfire deve invalidar o plano de 3 meses gratuitos do usuário premium.")]
        public void Should_invalidate_3_free_months_plan_premium_users()
        {
            int ExpiredPremiumUsersCount = 4;
            int ValidPremiumUsersCount = 5;

            var expiredPremiumUsers = new Faker<Account>()
                .RuleFor(x => x.IsPremium, x => true)
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddMonths(-4))
                .Generate(ExpiredPremiumUsersCount);

            var validPremiumUsers = new Faker<Account>()
                .RuleFor(x => x.IsPremium, x => true)
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddMonths(-2))
                .Generate(ValidPremiumUsersCount);

            var allPremiums = expiredPremiumUsers.Concat(validPremiumUsers);

            accountRepository.Setup(x => x.GetAll()).Returns(allPremiums);

            service.UpdateUsersPlanExpiration();

            var invalidPremiumUsers = allPremiums.Where(x => x.IsPlanExpired).Count();

            Assert.Equal(ExpiredPremiumUsersCount, invalidPremiumUsers);
        }

        [Fact(DisplayName = "Deve invalidar planos gratuitos, planos mensais, planos semestrais e planos anuais expirados.")]
        public void Should_invalidate_users_with_expired_plans()
        {
            int ExpiredMonthlyPlan = 1;
            int ValidMonthlyPlan = 1;

            int ExpiredSemesterPlan = 1;
            int ValidSemesterPlan = 1;

            int ExpiredAnualPlan = 1;
            int ValidAnualPlan = 1;

            int TotalExpired =
                ExpiredMonthlyPlan + ExpiredSemesterPlan + ExpiredAnualPlan;

            int TotalValid =
                ValidMonthlyPlan + ValidSemesterPlan + ValidAnualPlan;

            var expiredMonthlyPlanUsers = new Faker<Account>()
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanId, x => PlansConstants.Monthly)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddMonths(-4))
                .Generate(ExpiredMonthlyPlan);

            var validMonthlyPlanUsers = new Faker<Account>()
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanId, x => PlansConstants.Monthly)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddDays(-15))
                .Generate(ValidMonthlyPlan);

            var expiredSemesterPlanUsers = new Faker<Account>()
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanId, x => PlansConstants.Semester)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddMonths(-7))
                .Generate(ExpiredSemesterPlan);

            var validSemesterPlanUsers = new Faker<Account>()
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanId, x => PlansConstants.Semester)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddMonths(-5))
                .Generate(ValidSemesterPlan);

            var expiredAnualPlanUsers = new Faker<Account>()
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanId, x => PlansConstants.Anual)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddMonths(-13))
                .Generate(ExpiredAnualPlan);

            var validAnualPlanUsers = new Faker<Account>()
                .RuleFor(x => x.IsPlanExpired, x => false)
                .RuleFor(x => x.PlanId, x => PlansConstants.Anual)
                .RuleFor(x => x.PlanStartDate, x => DateTime.Now.AddMonths(-11))
                .Generate(ValidAnualPlan);

            var all = expiredMonthlyPlanUsers
                .Concat(validMonthlyPlanUsers)
                .Concat(expiredSemesterPlanUsers)
                .Concat(validSemesterPlanUsers)
                .Concat(expiredAnualPlanUsers)
                .Concat(validAnualPlanUsers);

            accountRepository.Setup(x => x.GetAll()).Returns(all);

            service.UpdateUsersPlanExpiration();

            var expiredPlans = all.Where(x => x.IsPlanExpired);
            var validPlans = all.Where(x => !x.IsPlanExpired);

            Assert.Equal(expiredPlans.Count(), TotalExpired);
            Assert.Equal(validPlans.Count(), TotalValid);
        }
    }
}
