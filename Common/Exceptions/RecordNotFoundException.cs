using System.Runtime.Serialization;

namespace Common.Exceptions
{
    public class RecordNotFoundException : Exception
    {
        /// <summary>
        /// Cria uma mensagem de erro customizada com base nos parâmetros providos. Por exemplo:
        /// <c>Investidor</c> de id <c>1</c> não encontrado na base de dados.
        /// </summary>
        /// <param name="record">O nome do tipo do registro. Por exemplo: <c>Investidor</c>, <c>Plano</c> ou <c>Preço médio</c>.</param>
        /// <param name="id">O id do registro.</param>
        public RecordNotFoundException(string record, string id) : base($"{record} de id {id} não encontrado na base de dados.") { }
        public RecordNotFoundException(string message) : base(message) { }
        public RecordNotFoundException() { }
        public RecordNotFoundException(string message, Exception innerException) : base(message, innerException) { }
        public RecordNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
