using System.ComponentModel;

namespace Common.Enums
{
    /// <summary>
    /// Representa os status de uma conta cadastrada.
    /// Os números representam uma sequência linear de eventos que uma conta obedece.
    /// </summary>
    public enum AccountStatus
    {
        /// <summary>
        /// O primeiro estágio linear de Status.
        /// 
        /// Quando um usuário é registrado na plataforma, ainda é necessário confirmar o endereço de e-mail.
        /// Enquanto o e-mail não é confirmado, o status <c>EmailNotConfirmed</c> é definido.
        /// </summary>
        [Description("EMAIL_NOT_CONFIRMED")] 
        EmailNotConfirmed = 0,

        /// <summary>
        /// O segundo e último estágio linear de Status.
        /// 
        /// É definido quando um usuário confirmou o seu endereço de e-mail.
        /// </summary>
        [Description("EMAIL_CONFIRMED")]
        EmailConfirmed = 1,

        /// <summary>
        /// Quando um plano é expirado, a conta é definida como <c>SUBSCRIPTION_EXPIRED</c>.
        /// </summary>
        [Description("SUBSCRIPTION_EXPIRED")]
        SubscriptionExpired = 2,

        /// <summary>
        /// Quando um plano ainda é válido, a conta é definida como <c>SUBSCRIPTION_VALID</c>.
        /// </summary>
        [Description("SUBSCRIPTION_VALID")]
        SubscriptionValid = 3
    }
}
