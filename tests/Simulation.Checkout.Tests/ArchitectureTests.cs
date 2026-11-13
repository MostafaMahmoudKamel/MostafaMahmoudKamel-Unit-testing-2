using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Application;
using Simulation.Checkout.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests;

public class ArchitectureTests
{
   
    // Rule 5.1: Domain does not depend on Application or Infrastructure
    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Application_Or_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Order).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Simulation.Checkout.Application", "Simulation.Checkout.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // ==========================================
    // Rule 5.2: Application does not depend on Infrastructure
    // ==========================================
    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CheckoutService).Assembly)
            .Should()
            .NotHaveDependencyOn("Simulation.Checkout.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // ==========================================
    [Fact]
    public void Interfaces_Should_Start_With_I()
    {
        var result = Types.InAssembly(typeof(Order).Assembly) // أو تديها الـ Base Assembly لكل المشروع
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // Rule 5.4: Classes implementing IOrderRepository end with 'Repository'
    [Fact]
    public void OrderRepository_Implementations_Should_End_With_Repository()
    {
        var result = Types.InAssembly(typeof(CheckoutService).Assembly) // أو الـ Infrastructure assembly
            .That()
            .ImplementInterface(typeof(Simulation.Checkout.Application.Abstractions.IOrderRepository))
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
