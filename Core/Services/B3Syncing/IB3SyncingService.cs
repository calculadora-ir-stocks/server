using Core.Requests.BigBang;

namespace Core.Services.B3Syncing
{
    public interface IB3SyncingService
    {
        /// <summary>
        /// Faz a sincronização da conta de um investidor com os serviços da B3.
        /// Ao sincronizar, todas as movimentações desde a data mínima de consulta (01/11/2019) até D-1
        /// são percorridas para salvar todos os impostos retroativos e preços médios de todos os ativos negociados.
        /// </summary>
        /// <param name="accountId">O id do usuário.</param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task Sync(Guid accountId, List<BigBangRequest> request);
    }
}
