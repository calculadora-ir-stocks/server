using System.Runtime.Serialization;

namespace Common.Exceptions
{
    public class BadGatewayException : Exception
    {
        public BadGatewayException(string message) : base(message) { }
        public BadGatewayException() { }
        public BadGatewayException(string message, Exception innerException) : base(message, innerException) { }
        public BadGatewayException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
