namespace stocks_core.Services.PremiumCode
{
    public interface IPremiumCodeService
    {
        /// <summary>
        /// Retorna verdadeiro caso o código de pré-lançamento seja válido, falso caso contrário.
        /// </summary>
        bool IsValid(string code);
    }
}
