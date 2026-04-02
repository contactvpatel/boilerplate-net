using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using WebShop.Api.Controllers;
using WebShop.Business;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Extensions;
using Xunit;

namespace WebShop.ArchitectureTests;

/// <summary>
/// Enforces naming conventions across all layers.
/// Consistent naming improves discoverability and makes the codebase self-documenting.
/// </summary>
public class NamingConventionTests
{
    private static readonly Assembly CoreAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly BusinessAssembly = typeof(DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(DataAccessExtensions).Assembly;
    private static readonly Assembly ApiAssembly = typeof(BaseApiController).Assembly;

    #region Interface prefix convention

    [Fact]
    public void CoreInterfaces_ShouldStartWith_I()
    {
        var result = Types.InAssembly(CoreAssembly)
            .That().AreInterfaces()
            .Should().HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all interfaces in WebShop.Core must follow the 'I' prefix convention");
    }

    [Fact]
    public void BusinessInterfaces_ShouldStartWith_I()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .That().AreInterfaces()
            .Should().HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all interfaces in WebShop.Business must follow the 'I' prefix convention");
    }

    [Fact]
    public void InfrastructureInterfaces_ShouldStartWith_I()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().AreInterfaces()
            .Should().HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all interfaces in WebShop.Infrastructure must follow the 'I' prefix convention");
    }

    #endregion

    #region Service naming

    [Fact]
    public void BusinessServices_ShouldEndWith_Service()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .That().ResideInNamespace("WebShop.Business.Services")
            .And().AreClasses()
            .And().AreNotAbstract()
            .Should().HaveNameEndingWith("Service")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all concrete service classes in WebShop.Business.Services must end with 'Service'");
    }

    [Fact]
    public void BusinessServiceInterfaces_ShouldResideIn_InterfacesNamespace()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .That().AreInterfaces()
            .And().HaveNameEndingWith("Service")
            .Should().ResideInNamespace("WebShop.Business.Services.Interfaces")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all service interfaces in WebShop.Business must reside in the WebShop.Business.Services.Interfaces namespace");
    }

    #endregion

    #region Repository naming

    [Fact]
    public void InfrastructureRepositories_ShouldEndWith_Repository()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().ResideInNamespace("WebShop.Infrastructure.Repositories")
            .And().AreClasses()
            .And().ArePublic()
            .And().AreNotAbstract()
            .Should().HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all public repository classes in WebShop.Infrastructure.Repositories must end with 'Repository'");
    }

    #endregion

    #region Validator naming

    [Fact]
    public void BusinessValidators_ShouldEndWith_Validator()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .That().ResideInNamespace("WebShop.Business.Validators")
            .And().AreClasses()
            .Should().HaveNameEndingWith("Validator")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all validator classes in WebShop.Business.Validators must end with 'Validator'");
    }

    #endregion

    #region Controller naming

    [Fact]
    public void ApiControllers_ShouldEndWith_Controller()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("WebShop.Api.Controllers")
            .And().AreClasses()
            .And().AreNotAbstract()
            .Should().HaveNameEndingWith("Controller")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all concrete controller classes in WebShop.Api.Controllers must end with 'Controller'");
    }

    #endregion

    #region External client naming

    [Fact]
    public void InfrastructureExternalServices_ShouldEndWith_Service()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().ResideInNamespace("WebShop.Infrastructure.Services.External")
            .And().AreClasses()
            .Should().HaveNameEndingWith("Service")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all external service classes in WebShop.Infrastructure.Services.External must end with 'Service'");
    }

    #endregion

    #region DTO namespace residency

    [Fact]
    public void BusinessDTOs_ShouldResideIn_DTOsNamespace()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .That().HaveNameEndingWith("Dto")
            .Should().ResideInNamespace("WebShop.Business.DTOs")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all DTO classes in WebShop.Business must reside in the WebShop.Business.DTOs namespace");
    }

    #endregion
}
