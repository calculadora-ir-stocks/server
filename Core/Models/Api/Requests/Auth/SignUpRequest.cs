namespace Api.DTOs.Auth
{
    public record SignUpRequest
    {
        public SignUpRequest(string auth0Id, string cpf, string birthDate, bool isTOSAccepted)
        {
            Auth0Id = auth0Id;
            CPF = cpf;
            BirthDate = birthDate;
            IsTOSAccepted = isTOSAccepted;
        }

        public string Auth0Id { get; init; }
        public string CPF { get; init; }
        public string BirthDate { get; init; }
        public bool IsTOSAccepted { get; init; }
    }
}
