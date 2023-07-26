namespace common.Models;

public class OperationDetails
{
    public OperationDetails(
        string day,
        string tickerSymbol,
        string corporationName,
        string dayOfTheWeek,
        string operation,
        int quantity,
        double value,
        double profit
    )
    {
        Day = day;
        TickerSymbol = tickerSymbol;
        CorporationName = corporationName;
        DayOfTheWeek = dayOfTheWeek;
        Operation = operation;
        Quantity = quantity;
        Value = value;
        Profit = profit;
    }

    public string Day { get; protected set; }
    public string TickerSymbol { get; init; }
    public string CorporationName { get; init; }
    public string DayOfTheWeek { get; init; }
    public string Operation { get; init; }
    public int Quantity { get; init; }
    public double Value { get; init; }
    public double Profit { get; protected set; } = 0;
}
