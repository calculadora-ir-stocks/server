namespace Common.Models
{
    public class JwtContent
    {
        public JwtContent(Guid id, string status)
        {
            Id = id;
            Status = status;
        }

        public Guid Id { get; init; }
        public string Status { get; init; }
    }
}
