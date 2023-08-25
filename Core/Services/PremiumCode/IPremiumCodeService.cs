namespace Core.Services.PremiumCode
{
    public interface IPremiumCodeService
    {
        /// <summary>
        /// Retorna verdadeiro caso o código de pré-lançamento exista, falso caso contrário.
        /// </summary>
        bool IsValid(string code);

        /// <summary>
        /// Retorna verdadeiro caso o código de pré-lançamento seja válido, falso caso contrário.
        /// </summary>
        bool Active(string code);

        /// <summary>
        /// Remove da base de dados um código de pré-lançamento.
        /// Deve ser utilizado após um usuário usá-lo.
        /// </summary>
        void DeactivatePremiumCode(string code);
    }
}
