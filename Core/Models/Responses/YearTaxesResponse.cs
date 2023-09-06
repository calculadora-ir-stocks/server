namespace Core.Models.Responses;

public class YearTaxesResponse
{
    public YearTaxesResponse(string month, double taxes, double swingTradeProfit, double dayTradeProfit)
    {
        Month = month;
        Taxes = taxes;
        SwingTradeProfit = swingTradeProfit;
        DayTradeProfit = dayTradeProfit;
    }

    public string Month { get; }
    public double Taxes { get; }
    public double SwingTradeProfit { get; }
    public double DayTradeProfit { get; }
}