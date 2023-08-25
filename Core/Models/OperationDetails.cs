namespace stocks_core.Models;

public class OperationDetails
{
    public OperationDetails(
        string day,
        string dayOfTheWeek,
        string tickerSymbol,
        string corporationName,
        string operation,
        int quantity,
        double value,
        double profit
    )
    {
        Day = day;
        DayOfTheWeek = dayOfTheWeek;
        TickerSymbol = tickerSymbol;
        CorporationName = corporationName;
        Operation = operation;
        Quantity = quantity;
        Value = value;
        Profit = profit;
    }

    public string Day { get; protected set; }
    public string DayOfTheWeek { get; init; }
    public string TickerSymbol { get; init; }
    public string CorporationName { get; init; }
    public string Operation { get; init; }
    public int Quantity { get; init; }
    public double Value { get; init; }
    public double Profit { get; protected set; } = 0;
}
