using System.Runtime.Serialization;

namespace Common.Exceptions
{
    /// <summary>
    /// Exceção que retorna <c>Status Code 404 Not Found</c>,
    /// </summary>
    public class NotFoundException : Exception
    {
        /// <summary>
        /// Cria uma mensagem de erro customizada com base nos parâmetros providos. Por exemplo:
        /// <c>Investidor</c> de id <c>1</c> não encontrado na base de dados.
        /// </summary>
        /// <param name="record">O nome do tipo do registro. Por exemplo: <c>Investidor</c>, <c>Plano</c> ou <c>Preço médio</c>.</param>
        /// <param name="id">O id do registro.</param>
        public NotFoundException(string record, string id) : base($"{record} de id {id} não encontrado na base de dados.") { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException() { }
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
        public NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
