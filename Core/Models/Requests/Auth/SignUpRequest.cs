﻿namespace Api.DTOs.Auth
{
    public record SignUpRequest
    {
        public SignUpRequest(string name, string email, string cPF, string birthDate, string password, string phoneNumber)
        {
            Name = name;
            Email = email;
            CPF = cPF;
            BirthDate = birthDate;
            Password = password;
            PhoneNumber = phoneNumber;
        }

        public string Name { get; init; }
        public string Email { get; init; }
        public string CPF { get; init; }
        public string BirthDate { get; init; }
        public string Password { get; init; }
        public string PhoneNumber { get; init; }
    }
}
