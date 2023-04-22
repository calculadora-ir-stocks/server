namespace stocks_infrastructure.Models
{
    public class Asset
    {
        public Asset(int id, string name)
        {
            Id = id;
            Name = name;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private Asset() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public int Id { get; set; }
        public string Name { get; set; }
    }
}
