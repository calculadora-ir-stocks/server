using Microsoft.Extensions.Hosting;

namespace stocks_core.BackgroundServices
{
    /// <summary>
    /// Background Service que roda todos os dias meia-noite para obter
    /// todas as movimentações do dia do investidor e, caso uma compra ou venda tenha sido realizada,
    /// atualiza o preço médio do determinado ativo. 
    /// </summary>
    public class UpdateAverageTradedPrice : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                int hourSpan = 24 - DateTime.Now.Hour;
                int numberOfHours = hourSpan;

                if (hourSpan == 24)
                {
                    // Atualiza o preço médio dos ativos do investidor.
                    numberOfHours = 24;
                }

                await Task.Delay(TimeSpan.FromHours(numberOfHours), stoppingToken);
            }
            while (!stoppingToken.IsCancellationRequested);
        }
    }
}
