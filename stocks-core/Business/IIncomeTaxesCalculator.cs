using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public interface IIncomeTaxesCalculator
    {
        /// <summary>
        /// Adiciona no objeto CalculateAssetsIncomeTaxesResponse os ativos e seus respectivos impostos de renda a serem pagos.
        /// </summary>
        Task AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId);

        /// <summary>
        /// Calcula o imposto de renda a ser pago em ativos que ainda não possuem o preço médio salvo na base de dados.
        /// Após calcular o imposto de renda, salva na base de dados o preço médio dos respectivos ativos.
        /// Deve ser executado uma única vez quando o usuário cadastrar-se na plataforma.
        /// </summary>
        void CalculateIncomeTaxesForTheFirstTimeAndSaveAverageTradedPrice(CalculateAssetsIncomeTaxesResponse response,
            IEnumerable<Movement.EquitMovement> movements, Guid accountId);
    }
}
