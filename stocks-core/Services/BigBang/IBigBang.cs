using stocks_core.DTOs.B3;
using stocks_core.Models;

namespace stocks_core.Services.BigBang
{
    /// <summary>
    /// Responsável por calcular o imposto de renda a ser pago em todos os meses de 01/11/2019 até D-1.
    /// Também salva na base de dados o preço médio de cada ativo.
    /// </summary>
    public interface IBigBang
    {
        /// <summary>
        /// Retorna o imposto de renda a ser pago nas movimentações especificadas e o preço médio
        /// de cada ativo movimentado.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Dictionary<string, List<AssetIncomeTaxes>> Calculate(Movement.Root? request);
    }
}
