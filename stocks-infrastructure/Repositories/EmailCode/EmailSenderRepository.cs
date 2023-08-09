using stocks.Database;
using stocks_infrastructure.Models;

namespace stocks_infrastructure.Repositories.EmailCode
{
    public class EmailSenderRepository : IEmailSenderRepository
    {
        private readonly StocksContext context;

        public EmailSenderRepository(StocksContext context)
        {
            this.context = context;
        }

        public async Task Create(string code, Account account)
        {
            await context.EmailCodes.AddAsync(new Models.EmailCode(code, account.Id, account));

            context.Attach(account);

            await context.SaveChangesAsync();
        }

        public void Delete(Models.EmailCode emailSender)
        {
            context.EmailCodes.Remove(emailSender);
            context.SaveChanges();
        }

        public Models.EmailCode? GetByAccountId(Guid accountId)
        {
            return context.EmailCodes.Where(x => x.AccountId == accountId).FirstOrDefault();            
        }
    }
}
