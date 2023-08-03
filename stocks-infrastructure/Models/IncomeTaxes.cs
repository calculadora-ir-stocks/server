using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stocks_infrastructure.Models
{
    /// <summary>
    /// Tabela para armazenar o imposto de renda a ser pago em um determinado mês.
    /// </summary>
    public class IncomeTaxes
    {
        public IncomeTaxes(string month, double totalTaxes, double totalSold, double swingTradeProfit, double dayTradeProfit,
            string tradedAssets, Account account, int assetId)
        {
            Month = month;
            TotalTaxes = totalTaxes;
            TotalSold = totalSold;
            SwingTradeProfit = swingTradeProfit;
            DayTradeProfit = dayTradeProfit;
            TradedAssets = tradedAssets;
            CompesatedSwingTradeLoss = swingTradeProfit > 0 ? false : null;
            CompesatedDayTradeLoss = dayTradeProfit > 0 ? false : null;
            Account = account;
            AssetId = assetId;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IncomeTaxes() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();

        public string Month { get; set; }

        /// <summary>
        /// https://www.youtube.com/watch?v=VMwqYLSPg_c
        /// </summary>
        public double TotalTaxes { get; set; }

        public double TotalSold { get; set; }

        /// <summary>
        /// Define se o imposto do mês específico já foi pago.
        /// </summary>
        public bool Paid { get; set; } = false;

        public double SwingTradeProfit { get; set; }

        public double DayTradeProfit { get; set; }

        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string TradedAssets { get; set; }

        /// <summary>
        /// Define se o prejuízo de swing-trade já foi compensado em algum pagamento. É NULL caso
        /// o investidor não tenha tido prejuízo no mês.
        public bool? CompesatedSwingTradeLoss { get; set; }

        /// <summary>
        /// Define se o prejuízo de day-trade já foi compensado em algum pagamento. É NULL caso
        /// o investidor não tenha tido prejuízo no mês.
        public bool? CompesatedDayTradeLoss { get; set; }

        public Account Account { get; set; }

        /// <summary>
        /// Id do tipo de ativo que foi negociado no mês.
        /// </summary>
        public int AssetId { get; set; }
    }
}
