namespace Infrastructure.Models
{
    public class PremiumCode : BaseEntity
    {
        // 4-digit random number
        public string Code { get; private set; } = new Random().Next(1000, 9999).ToString();
        public bool IsActive { get; set; } = true;
    }
}
