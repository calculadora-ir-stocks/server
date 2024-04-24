namespace Core.Hangfire.PlanExpirer
{
    public interface IPlanExpirerHangfire
    {
        /// <summary>
        /// Expira o plano de um usuário caso ele esteja expirado.
        /// </summary>
        Task Execute();
    }
}
