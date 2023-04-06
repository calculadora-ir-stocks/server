namespace stocks.Requests
{
    /// <summary>
    /// Preço médio de um determinado ativo que foi comprado antes de 01/11/2019 - a data mínima de consulta da B3.
    /// </summary>
    public class CalculateIncomeTaxesForEveryMonthRequest
    {
        public string Ticker { get; set; }
        public double AverageTradedPrice { get; set; }
    }
}
