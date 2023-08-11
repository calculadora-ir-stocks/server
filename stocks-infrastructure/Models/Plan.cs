namespace stocks_infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Plan
    {
        public Plan(int id, string name, string description, double price, int durationInMonths)
        {
            Id = id;
            Name = name;
            Description = description;
            Price = price;
            DurationInMonths = durationInMonths;
        }

        public Plan()
        {
        }

        public int Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public double Price { get; init; }
        public int DurationInMonths { get; init; }
    }
}
