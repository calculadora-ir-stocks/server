namespace stocks_common.Models
{
    public class CalculateIncomeTaxesForTheFirstTime
    {
        public CalculateIncomeTaxesForTheFirstTime(double currentPrice, double currentQuantity, string tickerSymbol, string tradeDateTime,
            double averageTradedPrice, bool tickerBoughtBeforeB3DateRange = false)
        {
            Price = currentPrice;
            Quantity = currentQuantity;
            TickerSymbol = tickerSymbol;
            TradeDateTime = tradeDateTime;
            AverageTradedPrice = averageTradedPrice;
            TickerBoughtBeforeB3DateRange = tickerBoughtBeforeB3DateRange;
        }

        public double Price { get; set; }
        public double Quantity { get; set; }
        public string TickerSymbol { get; set; }
        public string TradeDateTime { get; set; }
        public double AverageTradedPrice { get; set; } = 0;
        public double Profit { get; set; } = 0;
        public bool TickerBoughtBeforeB3DateRange { get; set; } = false;
        /// <summary>
        /// Valor a ser compensado no total a ser pago de imposto de renda. Esse valor refere-se as taxas de IRRF (ex.: dedo-duro).
        /// </summary>
        public double ValueToBeCompensated { get; set; } = 0;
        public bool DayTraded { get; set; } = false;
    }
}
