namespace stocks_core.Constants
{
    /// <summary>
    /// Classe responsável por armazenar os diferentes tipos de valores que a propriedade 'productTypeName', obtido como response através da API 
    /// da B3 '/api/movement/v2/equities/investors/...' possui.
    /// </summary>
    public static class AssetMovementTypes
    {
        public const string Stocks = "Ações";
        public const string ETFs = "ETF - Exchange Traded Fund";
        public const string Gold = "Ouro";
        public const string FIIs = "FII - Fundo de Investimento Imobiliário";
        public const string FundInvestments = "Fundos de Investimentos";
        public const string BDRs = "BDR - Brazilian Depositary Receipts";
    }
}
