namespace Common
{
    public class EnvironmentVariableInitializer
    {
        public static void Load(string envFileOnRoot)
        {
            if (!File.Exists(envFileOnRoot))
                throw new FileNotFoundException("O arquivo .env especificado não foi encontrado");

            foreach (var line in File.ReadAllLines(envFileOnRoot))
            {
                var parts = line.Split(
                    '=',
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }
}
