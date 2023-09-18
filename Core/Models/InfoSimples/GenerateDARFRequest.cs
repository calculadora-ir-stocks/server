namespace Core.Models.InfoSimples
{
    public class GenerateDARFRequest
    {
        public GenerateDARFRequest
        (            
            string cPF,
            string birthDate,
            string observacoes,
            string codigo,
            double valorPrincipal,
            string periodoApuracao,
            string dataConsolidacao
        )
        {            
            CPF = cPF;
            BirthDate = birthDate;
            Observacoes = observacoes;
            Codigo = codigo;
            ValorPrincipal = valorPrincipal;
            PeriodoApuracao = periodoApuracao;
            DataConsolidacao = dataConsolidacao;
        }

        /// <summary>
        /// O CPF do investidor sem pontuação e hífen.
        /// </summary>
        public string CPF { get; init; }

        /// <summary>
        /// Data de nascimento do investidor sem barra.
        /// </summary>
        public string BirthDate { get; init; }

        /// <summary>
        /// Observações a serem inseridas na DARF.
        /// </summary>
        public string Observacoes { get; init; }

        /// <summary>
        /// O código da DARF.
        /// </summary>
        public string Codigo { get; init; }

        /// <summary>
        /// O valor total de imposto a ser pago.
        /// </summary>
        public double ValorPrincipal { get; init; }

        /// <summary>
        /// A data em que as operações foram feitas no formato mm/yyyy.
        /// </summary>
        public string PeriodoApuracao { get; init; }

        /// <summary>
        /// A data em que essa DARF está sendo gerada no formato dd/mm/yyyy.
        /// </summary>
        public string DataConsolidacao { get; init; }
    }
}
