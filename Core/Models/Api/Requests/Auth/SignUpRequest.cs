﻿namespace Api.DTOs.Auth
{
    public record SignUpRequest
    {
        public SignUpRequest(string auth0Id, string cpf, string birthDate, string phoneNumber, bool isTOSAccepted)
        {
            Auth0Id = auth0Id;
            CPF = cpf;
            BirthDate = birthDate;
            PhoneNumber = phoneNumber;
            IsTOSAccepted = isTOSAccepted;
        }

        public string Auth0Id { get; init; }
        public string CPF { get; init; }
        public string BirthDate { get; init; }
        public string PhoneNumber { get; init; }
        public bool IsTOSAccepted { get; init; }
    }
}
