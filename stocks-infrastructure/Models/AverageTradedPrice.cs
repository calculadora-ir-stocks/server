using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace stocks_infrastructure.Models
{
    /// <summary>
    /// Tabela para armazenar o preço médio de um determinado ativo de um investidor.
    /// A B3 não disponibiliza o preço médio dos ativos - eles precisam ser calculados manualmente.
    /// </summary>
    public class AverageTradedPrice
    {
        public AverageTradedPrice(string ticker, double averagePrice, int quantity, Account account, DateTime updatedAt)
        {
            Ticker = ticker;
            AveragePrice = averagePrice;
            Quantity = quantity;
            Account = account;
            UpdatedAt = updatedAt;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AverageTradedPrice()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public string Ticker { get; set; }
        public double AveragePrice { get; set; }
        public int Quantity { get; set; }
        public Account Account { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
