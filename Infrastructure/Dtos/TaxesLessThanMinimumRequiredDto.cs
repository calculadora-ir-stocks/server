namespace Infrastructure.Dtos
{
    public class TaxesLessThanMinimumRequiredDto
    {
        public TaxesLessThanMinimumRequiredDto(string month, double total)
        {
            Month = month;
            Total = total;
        }

        public string Month { get; init; }
        public double Total { get; init; }
    }
}
