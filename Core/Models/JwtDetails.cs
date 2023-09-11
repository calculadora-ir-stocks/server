namespace Common.Models
{
    public class JwtDetails
    {
        public JwtDetails(Guid id, bool isPlanExpired)
        {
            Id = id;
            IsPlanExpired = isPlanExpired;
        }

        public Guid Id { get; init; }
        public bool IsPlanExpired { get; init; }
    }
}
