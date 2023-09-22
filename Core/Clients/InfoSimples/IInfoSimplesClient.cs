using Core.Models.InfoSimples;

namespace Core.Clients.InfoSimples
{
    public interface IInfoSimplesClient
    {
        /// <summary>
        /// Faz uma requisição para a criação de DARF da Infosimples.
        /// </summary>
        Task<GenerateDARFResponse> GenerateDARF(GenerateDARFRequest request);
    }
}
