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
        EmailNotConfirmed = 0,
        /// <summary>
        /// O segundo estágio linear de Status.
        /// 
        /// É definido quando um usuário confirmou o seu endereço de e-mail.
        /// </summary>
        EmailConfirmed = 1,
        /// <summary>
        /// O terceiro estágio linear de Status
        /// 
        /// É definido quando um usuário executa o Big Bang pela primeira vez.
        /// Enquanto os preços médios e os impostos ainda estão sendo calculados, esse status é utilizado.
        /// </summary>
        B3APIDataSyncNotDone = 2,
        /// <summary>
        /// O último estágio linear de Status.
        /// 
        /// Quando os preços médios e os impostos ainda estão sendo calculados, esse status é utilizado.
        /// </summary>
        B3APIDataSyncDone = 3
    }
}
