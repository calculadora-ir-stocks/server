namespace Common.Options
{
    /// <summary>
    /// Representa uma instância da chave de criptografia utilizada pelo pgcrypto;
    /// </summary>
    public class DatabaseEncryptionKeyOptions
    {
#pragma warning disable CS8618 // Configured as an Option at DI.
        public string Value { get; set; }
#pragma warning restore CS8618 
    }
}
