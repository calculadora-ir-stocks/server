using Infrastructure.Repositories.EmailCode;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Core.Services.Hangfire.EmailCodeRemover
{
    public class EmailCodeRemoverHangfire : IEmailCodeRemoverHangfire
    {
        private readonly IEmailCodeRepository repository;
        private readonly ILogger<EmailCodeRemoverHangfire> logger;

        private const int MinutesToDelete = 10;

        public EmailCodeRemoverHangfire(IEmailCodeRepository repository, ILogger<EmailCodeRemoverHangfire> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public void Execute()
        {
            try
            {
                Guid threadId = new();

                logger.LogInformation("Iniciando Hangfire para remover códigos de confirmação vencidos." +
                    "Id do processo: {id}", threadId);

                Stopwatch timer = new();
                timer.Start();

                var emailCodes = repository.GetAll();

                foreach (var emailCode in emailCodes)
                {
                    if (emailCode.CreatedAt <= DateTime.UtcNow.AddMinutes(-MinutesToDelete))
                    {
                        repository.Delete(emailCode);
                    }
                }

                timer.Stop();
                var timeTaken = timer.Elapsed;

                logger.LogInformation("Finalizado Hangfire para remover códigos de confirmação vencidos." +
                    "Tempo de execução: {timeTaken}. Id do processo: {id}", timeTaken, threadId);

            } catch
            {
                throw;
            }
        }
    }
}
