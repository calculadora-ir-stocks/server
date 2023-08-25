using common.Helpers;

namespace stocks_infrastructure.Dtos;

public class SpecifiedYearTaxesDto
{
    private string month;

    public string Month
    {
        get
        {
            return UtilsHelper.GetMonthName(int.Parse(month));
        }

        set
        {
            month = value;
        }
    }
    public double Taxes { get; set; }
    public double SwingTradeProfit { get; set; }
    public double DayTradeProfit { get; set; }
}
