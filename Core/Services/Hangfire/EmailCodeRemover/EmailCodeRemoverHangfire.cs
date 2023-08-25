using stocks_infrastructure.Repositories.EmailCode;

namespace stocks_core.Services.Hangfire.EmailCodeRemover
{
    public class EmailCodeRemoverHangfire : IEmailCodeRemoverHangfire
    {
        private readonly IEmailCodeRepository repository;

        private const int MinutesToDelete = 10;

        public EmailCodeRemoverHangfire(IEmailCodeRepository repository)
        {
            this.repository = repository;
        }

        public void Execute()
        {
            try
            {
                var emailCodes = repository.GetAll();

                foreach (var emailCode in emailCodes)
                {
                    if (emailCode.CreatedAt <= DateTime.UtcNow.AddMinutes(-MinutesToDelete))
                    {
                        repository.Delete(emailCode);
                    }
                }
            } catch
            {
                throw;
            }
        }
    }
}
