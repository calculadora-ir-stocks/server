using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace common.Helpers;

public class DayOfTheWeekHelper
{

    private static readonly Dictionary<int, string> DayOfTheWeeks = new()
    {
        { 0, "Domingo" },
        { 1, "Segunda-feira" },
        { 2, "Terça-feira" },
        { 3, "Quarta-feira" },
        { 4, "Quinta-feira" },
        { 5, "Sexta-feira" },
        { 6, "Sábado" }
    };

    public static string GetDayOfTheWeek(int dayOfTheWeek)
    {
        return DayOfTheWeeks[dayOfTheWeek];
    }
}
