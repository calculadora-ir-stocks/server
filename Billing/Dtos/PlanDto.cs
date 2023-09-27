﻿namespace Billing.Dtos
{
    public class PlanDto
    {
        public PlanDto(string id, string name, long? price)
        {
            Id = id;
            Name = name;
            Price = price;
        }

        public string Id { get; init; }
        public string Name { get; init; }
        public long? Price { get; init; }
    }
}
