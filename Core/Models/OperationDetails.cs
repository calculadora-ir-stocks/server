using Common.Enums;

namespace Core.Models;

public class OperationDetails
{
    public OperationDetails(
        int day,
        string dayOfTheWeek,
        string assetType,
        Asset assetTypeId,
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
        AssetType = assetType;
        AssetTypeId = assetTypeId;
        TickerSymbol = tickerSymbol;
        CorporationName = corporationName;
        MovementType = operation;
        Quantity = quantity;
        Total = value;
        Profit = profit;
    }

    public int Day { get; init; }
    public string DayOfTheWeek { get; init; }
    public string AssetType { get; init; }
    public Asset AssetTypeId { get; init; }
    public string TickerSymbol { get; init; }
    public string CorporationName { get; init; }
    public string MovementType { get; init; }
    public int Quantity { get; init; }
    public double Total { get; init; }
    public double Profit { get; init; } = 0;
}
