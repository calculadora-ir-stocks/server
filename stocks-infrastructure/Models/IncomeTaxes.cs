using stocks.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stocks_infrastructure.Models
{
    /// <summary>
    /// Tabela para armazenar o imposto de renda a ser pago em um determinado mês.
    /// </summary>
    public class IncomeTaxes
    {
        public IncomeTaxes(string month, double totalTaxes, double totalSold, double totalProfit, bool dayTraded,
            string tradedAssets, bool? compesatedLoss, Account account, int assetId)
        {
            Month = month;
            TotalTaxes = totalTaxes;
            TotalSold = totalSold;
            TotalProfit = totalProfit;
            DayTraded = dayTraded;
            TradedAssets = tradedAssets;
            CompesatedLoss = compesatedLoss;
            Account = account;
            AssetId = assetId;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private IncomeTaxes() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public string Month { get; set; }
        public double TotalTaxes { get; set; }
        public double TotalSold { get; set; }
        public double TotalProfit { get; set; }
        public bool DayTraded { get; set; }
        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string TradedAssets { get; set; }
        /// <summary>
        /// Define se o prejuízo já foi compensado em algum pagamento. É NULL caso
        /// o investidor não tenha tido prejuízo no mês.
        public bool? CompesatedLoss { get; set; }
        public Account Account { get; set; }
        /// <summary>
        /// Id do ativo que foi negociado no mês.
        /// </summary>
        public int AssetId { get; set; }
    }
}
