using System.Text.RegularExpressions;

namespace common.Helpers;

public class UtilsHelper
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

    private static readonly Dictionary<int, string> Months = new()
    {
        { 1, "Janeiro" },
        { 2, "Fevereiro" },
        { 3, "Março" },
        { 4, "Abril" },
        { 5, "Maio" },
        { 6, "Junho" },
        { 7, "Julho" },
        { 8, "Agosto" },
        { 9, "Setembro" },
        { 10, "Outubro" },
        { 11, "Novembro" },
        { 12, "Dezembro" }
    };

    /// <summary>
    /// Retorna o nome do dia da semana com base no respectivo número.
    /// </summary>
    public static string GetDayOfTheWeekName(int dayOfTheWeek)
    {
        return DayOfTheWeeks[dayOfTheWeek];
    }

    /// <summary>
    /// Retorna o nome do mês com base no respectivo número.
    /// </summary>
    public static string GetMonthName(int month)
    {
        return Months[month];
    }

    /// <summary>
    /// Retorna o nome do mês e ano ano com base no parâmetro
    /// </summary>
    /// <param name="monthYear">Formato: MM/yyyy</param>
    public static string GetMonthAndYearName(string monthYear)
    {
        string month = monthYear.Substring(0, 2);
        string year = monthYear.Substring(monthYear.Length - 4);

        return $"{GetMonthName(int.Parse(month))}, {year}";
    }

    /// <summary>
    /// Remove caracteres especiais de uma string.
    /// </summary>
    /// <returns>String sem caracteres especiais.</returns>
    public static string RemoveSpecialCharacters(string str)
    {
        return Regex.Replace(str, "[^a-zA-Z0-9]", "", RegexOptions.Compiled);
    }
}
