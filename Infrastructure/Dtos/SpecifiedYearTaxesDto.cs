using common.Helpers;

namespace Infrastructure.Dtos;

public class SpecifiedYearTaxesDto
{
    public string Month { get; set; }
    public double Taxes { get; set; }
    public double SwingTradeProfit { get; set; }
    public double DayTradeProfit { get; set; }
}
