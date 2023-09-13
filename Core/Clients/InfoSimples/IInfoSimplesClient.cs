using Core.Models.InfoSimples;

namespace Core.Clients.InfoSimples
{
    public interface IInfoSimplesClient
    {
        Task<GenerateDARFResponse> GenerateDARF(GenerateDARFRequest request);
    }
}
