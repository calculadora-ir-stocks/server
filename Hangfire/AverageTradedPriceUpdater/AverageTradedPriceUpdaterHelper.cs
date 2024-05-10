using Core.Models;
using Infrastructure.Dtos;
using static Core.Models.B3.Movement;

namespace Hangfire.AverageTradedPriceUpdater
{
    public static class AverageTradedPriceUpdaterHelper
    {
        /// <summary>
        /// Baseado em ativos negociados e ativos já possuídos, retorna um <c>IEnumerable<string></c> contendo os
        /// ativos a serem adicionados na carteira do investidor.
        /// </summary>
        /// <param name="tradedTickers">Ativos negociados em um intervalo de tempo.</param>
        /// <param name="allTickers">Todos os ativos do investidor.</param>
        /// <returns><c>IEnumerable<string></c> contendo o nome dos ativos a serem adicionados na carteira do investidor.</returns>
        public static IEnumerable<string> GetTickersToAdd(List<AverageTradedPriceDetails> tradedTickers,
            IEnumerable<AverageTradedPriceDto> allTickers)
        {
            return tradedTickers.Select(x => x.TickerSymbol).ToList().Except(allTickers.Select(x => x.Ticker)).Distinct();
        }

        /// <summary>
        /// Baseado em ativos negociados e ativos já possuídos, retorna um <c>IEnumerable<string></c> contendo os
        /// ativos a serem atualizados na carteira do investidor.
        /// </summary>
        /// <param name="tradedTickers">Ativos negociados em um intervalo de tempo.</param>
        /// <param name="allTickers">Todos os ativos do investidor.</param>
        /// <returns><c>IEnumerable<string></c> contendo o nome dos ativos a serem atualizados na carteira do investidor.</returns>
        public static IEnumerable<string> GetTickersToUpdate(List<AverageTradedPriceDetails> tradedTickers,
            IEnumerable<AverageTradedPriceDto> allTickers)
        {

            var tickers = tradedTickers.Where(x => allTickers.Any(y => y.Ticker.Equals(x.TickerSymbol)));
            return tickers.Select(x => x.TickerSymbol).Distinct();
        }

        /// <summary>
        /// Baseado em ativos negociados e em determinadas movimentações, retorna um <c>IEnumerable<string></c> contendo os
        /// ativos a serem removidos na carteira do investidor pois foram completamente vendidos.
        /// </summary>
        /// <param name="tradedTickers">Ativos negociados em <para>movements</para>.</param>
        /// <param name="movements">As movimentações feitas em um intervalo de tempo.</param>
        /// <returns><c>IEnumerable<string></c> contendo o nome dos ativos a serem removidos na carteira do investidor.</returns>
        public static IEnumerable<string> GetTickersToRemove(List<AverageTradedPriceDetails> tradedTickers, List<EquitMovement> movements)
        {
            return movements.Where(m => !tradedTickers.Any(l => l.TickerSymbol == m.TickerSymbol)).Select(x => x.TickerSymbol).Distinct();
        }
    }
}
