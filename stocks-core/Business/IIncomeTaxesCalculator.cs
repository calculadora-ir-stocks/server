using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Adiciona no objeto CalculateAssetsIncomeTaxesResponse os ativos e seus respectivos impostos de renda a serem pagos.
        /// </summary>
        Task CalculateCurrentMonthIncomeTaxes(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId);

        /// <summary>
        /// Calcula o imposto de renda a ser pago em todos os meses de todos os ativos desde 01/01/2019 até D-1.
        /// Também calcula o preço médio atual de todos os ativos da carteira do investidor.
        /// 
        /// O método deve ser executado uma única vez quando o usuário registrar-se na plataforma - tendo visto
        /// que o preço médio atual estará atualizado.
        /// </summary>
        void CalculateIncomeTaxesForAllMonths(CalculateAssetsIncomeTaxesResponse response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId);
    }
}
