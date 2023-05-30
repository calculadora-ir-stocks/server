namespace stocks_core.Requests.BigBang
{
    /// <summary>
    /// Preço médio de um determinado ativo que foi comprado antes de 01/11/2019 - a data mínima de consulta da B3.
    /// </summary>
    public class BigBangRequest
    {
        public BigBangRequest(string ticker, double averageTradedPrice)
        {
            Ticker = ticker;
            AverageTradedPrice = averageTradedPrice;
        }

        public string Ticker { get; set; }
        public double AverageTradedPrice { get; set; }
    }
}
