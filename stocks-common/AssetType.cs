using stocks.Enums;
using System.Text.RegularExpressions;

namespace stocks_common
{
    public class AssetType
    {
        public static Asset ReturnsAssetType(string ticker)
        {
            bool lastCharacterIsALetter = char.IsLetter(ticker[^1]);

            if (lastCharacterIsALetter)
                ticker = RemoveLastCharacterFromTicker(ticker);

            int lastTwoDigits = default;

            if (LastCharactersAreTwoDigits(ticker))
                lastTwoDigits = int.Parse(ticker[^2..]);

            string tickerName = Regex.Replace(ticker, @"[^A-Z]+", string.Empty);

            if (lastTwoDigits >= 12 && lastTwoDigits <= 15)
            {
                return Asset.FII;
            }

            if (lastTwoDigits >= 31 && lastTwoDigits <= 35 || lastTwoDigits == 39)
            {
                return Asset.BDR;
            }

            if (lastTwoDigits == 11)
            {
                return AssetWith11OnTicker(tickerName);
            }

            return Asset.Stock;
        }

        private static bool LastCharactersAreTwoDigits(string ticker)
        {
            return char.IsNumber(ticker[^2]);
        }

        /// <summary>
        /// Retorna o tipo do ativo.
        /// </summary>
        /// <returns>ETF, Unit ou FII</returns>
        private static Asset AssetWith11OnTicker(string tickerName)
        {
            // Atualmente, há 3 tipos de ativos que compartilham do mesmo número 11 no ticker:
            // Exchange - Traded Funds (ETFs), Units, e Fundos Imobiliários (FIIs).
            // Cada um desses ativos possui uma alíquota de IR diferente, ou seja, eles precisam ser diferenciados.
            //
            // Na API da B3, o tipo de ativo não é especificado; por conta disso, uma verificação manual é necessária.

            foreach (string name in Assets.ETFs)
            {
                if (tickerName == name) return Asset.ETF;
            }

            foreach (string name in Assets.Units)
            {
                if (tickerName == name) return Asset.Unit;
            }

            return Asset.FII;
        }

        private static string RemoveLastCharacterFromTicker(string asset)
        {
            return asset.Remove(asset.Length - 1);
        }
    }
}
