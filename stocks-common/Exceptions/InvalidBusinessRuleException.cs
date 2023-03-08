using System.Runtime.Serialization;

namespace stocks.Exceptions
{
    public class InvalidBusinessRuleException : Exception
    {
        public InvalidBusinessRuleException(string message) : base(message) { }
        public InvalidBusinessRuleException() { }
        public InvalidBusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
        public InvalidBusinessRuleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
