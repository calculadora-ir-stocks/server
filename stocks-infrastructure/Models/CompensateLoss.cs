using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using stocks.Models;

namespace stocks_infrastructure.Models
{
    /// <summary>
    /// Tabela para armazenar os prejuízos a serem compensados por um investidor.
    /// Quando um prejuízo for compensado, ele deve ser excluído da base de dados.
    /// </summary>
    public class CompensateLoss
    {
        public CompensateLoss(double total, int month, Guid accountId)
        {
            Total = total;
            Month = month;
            AccountId = accountId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public double Total { get; protected set; }
        public int Month { get; protected set; }
        public Guid AccountId { get; protected set; }
        public Account Account { get; protected set; }
    }
}
