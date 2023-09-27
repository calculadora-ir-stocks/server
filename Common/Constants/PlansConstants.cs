namespace Common.Constants
{
    /// <summary>
    /// Constantes representando o nome dos planos do Stripe. 
    /// Devem ser sempre iguais aos títulos dos planos do Stripe.
    /// </summary>
    public class PlansConstants
    {
        public const string Free = "Gratuito";

        public const string Monthly = "Mensal";

        /// <summary>
        /// Plano semestral pago uma única vez (válido por 6 meses).
        /// </summary>
        public const string Semester = "Semestral";

        /// <summary>
        /// Plano anual pago uma única vez (válido por 1 ano).
        /// </summary>
        public const string Anual = "Anual";
    }
}
