using Infrastructure.Dtos;

namespace Core.Models.Responses
{
    public class DARFResponse
    {
        public DARFResponse(string barCode, double taxes, double fine, double interests, string? observation, IEnumerable<TaxesLessThanMinimumRequiredDto> months)
        {
            BarCode = barCode;
            Taxes = taxes;
            Fine = fine;
            Interests = interests;
            Observation = observation;
            MonthsToCompensate = months;
        }

        public string BarCode { get; init; }
        public double Taxes { get; init; }
        public double Fine { get; init; }
        public double Interests { get; init; }
        public string? Observation { get; init; }
        public IEnumerable<TaxesLessThanMinimumRequiredDto> MonthsToCompensate { get; init; }
    }
}
