namespace stocks_core.Constants
{
    public static class IncomeTaxesConstants
    {
        /// <summary>
        /// Teto de vendas para se isentar do pagamento do imposto de renda de ações.
        /// </summary>
        public const double LimitForStocksSelling = 20000;

        /// <summary>
        /// Alíquota para o pagamento de imposto de renda sob ações.
        /// </summary>
        public const int IncomeTaxesForStocks = 15;

        /// <summary>
        /// Alíquota para o pagamento de imposto de renda sob operações daytrade.
        /// </summary>
        public const int IncomeTaxesForDayTrade = 20;

        /// <summary>
        /// Alíquota para o pagamento de imposto de renda para Exchange Traded Fund (ETFs).
        /// </summary>
        public const int IncomeTaxesForETFs = 15;

        /// <summary>
        /// Alíquota para o pagamento de imposto de renda para Ouro.
        /// </summary>
        public const int IncomeTaxesForGold = 15;

        /// <summary>
        /// Alíquota para o pagamento de imposto de renda sob Fundos Imobiliários (FIIs).
        /// </summary>
        public const int IncomeTaxesForFIIs = 20;

        /// <summary>
        /// Alíquota para o pagamento de imposto de renda sob Brazilian Depositary Receipts (BDRs).
        /// </summary>
        public const int IncomeTaxesForBDRs = 15;

        /// <summary>
        /// Alíquota do IRRF - comumente chamado de dedo-duro - sob operações swing trade.
        /// </summary>
        public const double IRRFSwingTrade = 0.005;

        /// <summary>
        /// Alíquota do IRRF - comumente chamado de dedo-duro - sob operações day-trade.
        /// </summary>
        public const int IRRFDayTrade = 1;
    }
}
