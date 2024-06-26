using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Models
{
    /// <summary>
    /// Tabela para armazenar o imposto de renda a ser pago em um determinado mês.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class IncomeTaxes
    {
        public IncomeTaxes(string month, double taxes, double totalSold, double swingTradeProfit, double dayTradeProfit,
            string tradedAssets, Account account, int assetId)
        {
            // TODO override ToString method w this implementation
            Month = month;
            Taxes = taxes.ToString().Replace(',', '.');
            TotalSold = totalSold.ToString().Replace(',', '.');
            SwingTradeProfit = swingTradeProfit.ToString().Replace(',', '.');
            DayTradeProfit = dayTradeProfit.ToString().Replace(',', '.');
            TradedAssets = tradedAssets;
            CompesatedSwingTradeLoss = swingTradeProfit > 0 ? false : null;
            CompesatedDayTradeLoss = dayTradeProfit > 0 ? false : null;
            Account = account;
            AssetId = assetId;
        }

        public IncomeTaxes() { }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();

        /// <summary>
        /// Id do tipo de ativo.
        /// </summary>
        public int AssetId { get; init; }

        public string Month { get; init; }

        /// <summary>
        /// A quantidade de imposto referente a esse tipo de ativo.
        /// https://www.youtube.com/watch?v=VMwqYLSPg_c
        /// </summary>
        public string Taxes { get; set; }

        public string TotalSold { get; set; }

        /// <summary>
        /// Define se o imposto do mês específico já foi pago.
        /// </summary>
        public bool Paid { get; init; } = false;

        public string SwingTradeProfit { get; set; }

        public string DayTradeProfit { get; set; }

        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public string TradedAssets { get; init; }

        /// <summary>
        /// Define se o prejuízo de swing-trade já foi compensado em algum pagamento. É NULL caso
        /// o investidor não tenha tido prejuízo no mês.
        public bool? CompesatedSwingTradeLoss { get; init; }

        /// <summary>
        /// Define se o prejuízo de day-trade já foi compensado em algum pagamento. É NULL caso
        /// o investidor não tenha tido prejuízo no mês.
        public bool? CompesatedDayTradeLoss { get; init; }

        public Account Account { get; set; }
    }
}
