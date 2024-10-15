namespace Hangfire.IncomeTaxesAdder
{
    public interface IIncomeTaxesAdderHangfire
    {
        /// <summary>
        /// Método executado todo dia 01 de todo mês.
        /// Nele, os impostos do mês anterior são calculados e salvos na base de dados.
        /// 
        /// Isso ocorre por conta da sequência de eventos que acontecem dentro da aplicação.
        /// 
        /// Quando o usuário se registra no app, o Big Bang é executado. 
        /// O Big Bang obtém todas as movimentações do investidor desde 01/11/2019 até o último dia do mês anterior
        /// e calcula os impostos de todos os meses e, enquanto o faz, calcula o preço médio de cada ativo na carteira do investidor.
        /// 
        /// Depois de calculados, o preço médio e os impostos até o último dia do mês anterior são salvos na base de dados.
        /// 
        /// Quando o usuário está acessando os impostos do mês atual, a aplicação obtém os preços médios salvos até o último dia do mês anterior
        /// e edita os preços médios (se necessário) 'on the fly' - ou seja, enquanto calcula o imposto do mês atual. Nada é salvo ou alterado na base de dados.
        /// 
        /// Quando o mês atual acabar, esse job obtém os preços médios salvos agora há 2 meses atrás e, desde o dia 01 do agora mês anterior, salva os impostos na base de dados.
        /// </summary>
        
        // Isso ficou muito ruim...? Devo pedir desculpas...? :c
        Task Execute();
    }
}
