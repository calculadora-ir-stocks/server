using Core.Models.InfoSimples;

namespace Core.Clients.InfoSimples
{
    public interface IInfoSimplesClient
    {
        /// <summary>
        /// Faz uma requisição para a criação de DARF da Infosimples.
        /// </summary>
        /// <returns>O código de barras do DARF.</returns>
        Task<string> GetBarCodeFromDARF(GenerateDARFRequest request);
    }
}
