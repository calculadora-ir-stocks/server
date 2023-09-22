using System.Runtime.Serialization;

namespace Api.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message) { }
        public BadRequestException() { }
        public BadRequestException(string message, Exception innerException) : base(message, innerException) { }
        public BadRequestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
