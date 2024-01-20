using System.ComponentModel;

namespace Common.Enums
{
    public enum AccountStatus
    {
        /// <summary>
        /// Quando uma conta não realizou a sincronização inicial com a B3, a conta é definida como <c>NEED_TO_SYNC</c>.
        /// </summary>
        [Description("NEED_TO_SYNC")]
        NeedToSync = 1,

        /// <summary>
        /// Quando um plano ainda é válido, a conta é definida como <c>SUBSCRIPTION_VALID</c>.
        /// </summary>
        [Description("SUBSCRIPTION_VALID")]
        SubscriptionValid = 2,

        /// <summary>
        /// Quando um plano é expirado, a conta é definida como <c>SUBSCRIPTION_EXPIRED</c>.
        /// </summary>
        [Description("SUBSCRIPTION_EXPIRED")]
        SubscriptionExpired = 3
    }
}
