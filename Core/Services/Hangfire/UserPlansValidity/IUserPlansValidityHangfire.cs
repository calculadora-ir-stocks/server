namespace stocks_core.Services.Hangfire.UserPlansValidity
{
    /// <summary>
    /// Responsável por invalidar os planos pagos de usuários caso passe do prazo estipulado.
    /// </summary>
    public interface IUserPlansValidityHangfire
    {
        /// <summary>
        /// Atualiza se um usuário está ou não com o seu plano expirado.
        /// </summary>
        void UpdateUsersPlanExpiration();
    }
}
