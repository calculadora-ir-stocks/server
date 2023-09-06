using System.ComponentModel;

namespace Common.Enums
{
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
        /// O segundo estágio linear de Status.
        /// 
        /// É definido quando um usuário confirmou o seu endereço de e-mail.
        /// </summary>
        [Description("EMAIL_CONFIRMED")]
        EmailConfirmed = 1,
        /// <summary>
        /// O terceiro estágio linear de Status
        /// 
        /// É definido quando um usuário executa o Big Bang pela primeira vez.
        /// Enquanto os preços médios e os impostos ainda estão sendo calculados, esse status é utilizado.
        /// </summary>
        [Description("SYNCING")]
        Syncing = 2,
        /// <summary>
        /// O último estágio linear de Status.
        /// 
        /// Quando os preços médios e os impostos ainda estão sendo calculados, esse status é utilizado.
        /// </summary>
        [Description("SYNCED")]
        Synced = 3
    }
}
