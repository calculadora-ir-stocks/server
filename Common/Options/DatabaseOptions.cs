using System.ComponentModel.DataAnnotations;

namespace Common.Options
{
    public class DatabaseOptions
    {
        [Required]
#pragma warning disable CS8618 // Configured as an Option at DI.
        public string ConnectionString { get; set; }
#pragma warning restore CS8618
    }
}
