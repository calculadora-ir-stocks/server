using System.Runtime.Serialization;

namespace Common.Exceptions
{
    public class NoneMovementsException : Exception
    {
        public NoneMovementsException(string message) : base(message) { }
        public NoneMovementsException() { }
        public NoneMovementsException(string message, Exception innerException) : base(message, innerException) { }
        public NoneMovementsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
