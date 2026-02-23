using NetArchTest.Rules;
using WebShop.Api.Controllers;
using WebShop.Business.Services;
using WebShop.Core.Models;
using WebShop.Infrastructure.Repositories;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Architecture;

/// <summary>
/// Architecture tests that enforce Clean Architecture dependency rules and design conventions.
/// See: https://www.milanjovanovic.tech/blog/enforcing-software-architecture-with-architecture-tests
/// </summary>
[Trait("Category", TestCategories.Unit)]
public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly CoreAssembly = typeof(ApiErrorModel).Assembly;
    private static readonly System.Reflection.Assembly BusinessAssembly = typeof(CustomerService).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(CustomerRepository).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(CustomerController).Assembly;

    [Fact]
    public void Core_ShouldNot_HaveDependencyOnApiBusinessOrInfrastructure()
    {
        TestResult result = Types
            .InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("WebShop.Api", "WebShop.Business", "WebShop.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage("Core must not depend on Api, Business, or Infrastructure", result));
    }

    [Fact]
    public void Business_ShouldNot_HaveDependencyOnInfrastructureOrApi()
    {
        TestResult result = Types
            .InAssembly(BusinessAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("WebShop.Infrastructure", "WebShop.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage("Business must not depend on Infrastructure or Api", result));
    }

    [Fact]
    public void Infrastructure_Repositories_Should_HaveDependencyOnCore()
    {
        TestResult result = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .HaveDependencyOn("WebShop.Core")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage("Infrastructure types ending with Repository must depend on WebShop.Core", result));
    }

    [Fact]
    public void Api_Controllers_ShouldNot_HaveDependencyOnInfrastructureRepositories()
    {
        TestResult result = Types
            .InAssembly(ApiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .ShouldNot()
            .HaveDependencyOn("WebShop.Infrastructure.Repositories")
            .GetResult();

        Assert.True(result.IsSuccessful, GetFailureMessage("Controllers must not depend on WebShop.Infrastructure.Repositories", result));
    }

    private static string GetFailureMessage(string ruleDescription, TestResult result)
    {
        string failing = result.FailingTypes is null || result.FailingTypes.Count == 0
            ? " (no type list available)"
            : ": " + string.Join(", ", result.FailingTypes.Select(t => t.FullName));
        return ruleDescription + failing;
    }
}
