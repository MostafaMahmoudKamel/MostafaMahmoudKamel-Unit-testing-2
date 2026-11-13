using AutoFixture;
using Bogus;
using Simulation.Checkout.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests.TestData;

public class OrderDataHelper
{
    public static Order CreateFixtureOrder()
    {
        var fixture = new Fixture();                    //AutoFixture Library 
        fixture.Customize<OrderItem>(item => item
        .With(o => o.Quantity, 2)
        .With(o => o.UnitPrice, 0));

        return fixture.Create<Order>();
    }

    public static Order CreateBogousSeeder()
    {
        var faker = new Faker<Order>()
            .RuleFor(o => o.CustomerName, f => f.Name.FullName())
            .RuleFor(o => o.CustomerEmail, f => f.Internet.Email())
            .RuleFor(o => o.Items, f => new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Phone",
                    Quantity = 1,
                    UnitPrice = 500m
                }
            })
            .UseSeed(2026);


        return faker.Generate();

    }
}
