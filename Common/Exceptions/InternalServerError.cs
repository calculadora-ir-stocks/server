using System.Runtime.Serialization;

namespace Common.Exceptions
{
    public class InternalServerErrorException : Exception
    {
        public InternalServerErrorException(string message) : base(message) { }
        public InternalServerErrorException() { }
        public InternalServerErrorException(string message, Exception innerException) : base(message, innerException) { }
        public InternalServerErrorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
