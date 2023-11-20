using Common.Enums;

namespace Core.Models.Responses;

public class CalendarResponse
{
    public CalendarResponse(string month, double taxes, TaxesStatus status, double swingTradeProfit, double dayTradeProfit)
    {
        Month = month;
        Taxes = taxes;
        Status = status;
        SwingTradeProfit = swingTradeProfit;
        DayTradeProfit = dayTradeProfit;
    }

    public string Month { get; }
    public double Taxes { get; }
    public TaxesStatus Status { get; }
    public double SwingTradeProfit { get; }
    public double DayTradeProfit { get; }
}