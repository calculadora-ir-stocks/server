using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Models
{
    /// <summary>
    /// Tabela para armazenar todas as bonificações de ativos. A B3 não retorna o preço unitário de uma bonificação,
    /// por isso, a inserimos manualmente na base de dados através de um contrato com a fintz.com.br.
    /// Atualmente, essa tabela precisa ser mantida por nós. Acompanhamos os eventos corporativos que ocorrem na B3 e,
    /// em seguida, buscamos o fato relevante da empresa que bonificou para sabermos o preço unitário da bonificação.
    /// </summary>
    public class BonusShare
    {
        public BonusShare(string ticker, DateTime date, double price, float proportion)
        {
            Ticker = ticker;
            Date = date.ToString("yyyy-MM-dd");
            Price = price;
            Proportion = proportion;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public BonusShare()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
        }

        [Key]
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Ticker { get; init; }
        public string Date { get; init; }
        public double Price { get; init; }
        public float Proportion { get; init; }
    }
}