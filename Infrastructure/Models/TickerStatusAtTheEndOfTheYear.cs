using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public class TickerStatusAtTheEndOfTheYear
    {
        public TickerStatusAtTheEndOfTheYear()
        {
        }

        public TickerStatusAtTheEndOfTheYear(int year, string ticker, double averagePrice, double totalBought, int quantity, Account account)
        {
            Year = year;
            Ticker = ticker;
            AveragePrice = averagePrice;
            TotalBought = totalBought;
            Quantity = quantity;
            Account = account;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public int Year { get; init; }
        public string Ticker { get; init; }
        public double AveragePrice { get; init; }
        public double TotalBought { get; init; }
        public int Quantity { get; init; }
        public Account Account { get; init; }
    }
}
