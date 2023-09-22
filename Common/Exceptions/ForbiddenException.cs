using System.Runtime.Serialization;

namespace Common.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
        public ForbiddenException() { }
        public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
        public ForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
