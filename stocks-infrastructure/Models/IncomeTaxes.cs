using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using stocks_infrastructure.Enums;
using stocks.Models;

namespace stocks_infrastructure.Models
{
    /// <summary>
    /// Tabela para armazenar o imposto de renda a ser pago em um determinado mês.
    /// </summary>
    public class IncomeTaxes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public string Ticker { get; protected set; }
        public ProductType ProductType { get; protected set; }
        public double Total { get; protected set; }
        public int Month { get; protected set; }
        public bool AccountDayTraded { get; protected set; }
        public Account Account { get; protected set; }
        public Guid AccountId { get; protected set; }
    }
}
