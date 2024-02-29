namespace Common.Models.Secrets
{
    public class DatabaseSecret
    {
        public DatabaseSecret()
        {
            Host = Environment.GetEnvironmentVariable("POSTGRES_HOST")!;
            Username = Environment.GetEnvironmentVariable("POSTGRES_USER")!;
            Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")!;
            Database = Environment.GetEnvironmentVariable("POSTGRES_DB")!;
        }

        public string GetConnectionString()
        {
            return $"host={Host};port={Port};database={Database};username={Username};password={Password};";
        }

        public string Host { get; init; }
        public string Username { get; init; }
        public string Password { get; init; }
        public string Database { get; init; }
        public int Port = 5432;
    }
}
