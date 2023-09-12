namespace Common.Models
{
    public class JwtDetails
    {
        public JwtDetails(Guid id, string isPlanExpired)
        {
            Id = id;
            IsPlanExpired = isPlanExpired;
        }

        public Guid Id { get; init; }
        public string IsPlanExpired { get; init; }
    }
}
