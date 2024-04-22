namespace Infrastructure.Models
{
    public class Audit
    {
        public Audit(int id, string data, string eventType, DateTime? updatedAt)
        {
            Id = id;
            Data = data;
            EventType = eventType;
            UpdatedAt = updatedAt;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Audit()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        public int Id { get; init; }
        public string Data { get; init; } // stored as JSON
        public string EventType { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
