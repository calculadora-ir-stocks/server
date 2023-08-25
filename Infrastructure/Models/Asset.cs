namespace Infrastructure.Models
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class Asset
    {
        public Asset(int id, string name)
        {
            Id = id;
            Name = name;
        }

        private Asset() { }

        public int Id { get; set; }
        public string Name { get; set; }
    }
}
