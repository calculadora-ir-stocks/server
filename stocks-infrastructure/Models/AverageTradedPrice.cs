using stocks.Models;
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
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public string Ticker { get; set; }
        public double AveragePrice { get; set; }
        public int Quantity { get; set; }
        public Account Account { get; set; }
        public Guid AccountId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
