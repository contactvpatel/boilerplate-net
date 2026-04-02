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
/// Enforces the Clean Architecture dependency rule: dependencies only point inward.
///
/// Strict Clean Architecture layers (inner to outer):
///   Core -> Business -> Api
///                    -> Infrastructure
///
/// Allowed dependency directions (-> means "depends on"):
///   Business       -> Core
///   Infrastructure -> Core, Business
///   Api            -> Core, Business
///   Api            -> Infrastructure  (pragmatic exception: the composition root
///                                      in Program.cs must reference Infrastructure
///                                      to register DI services. No application
///                                      logic in Api should depend on Infrastructure
///                                      types directly.)
///
/// Forbidden directions: Core, Business, or Infrastructure must never reference Api;
/// Business and Core must never reference Infrastructure.
/// </summary>
public class DependencyTests
{
    private static readonly Assembly CoreAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly BusinessAssembly = typeof(DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(DataAccessExtensions).Assembly;
    private static readonly Assembly ApiAssembly = typeof(BaseApiController).Assembly;

    #region Forbidden inward dependencies

    [Fact]
    public void Core_ShouldNotDependOn_BusinessLayer()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("WebShop.Business")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Core is the innermost layer and must not reference WebShop.Business");
    }

    [Fact]
    public void Core_ShouldNotDependOn_InfrastructureLayer()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("WebShop.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Core is the innermost layer and must not reference WebShop.Infrastructure");
    }

    [Fact]
    public void Core_ShouldNotDependOn_ApiLayer()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("WebShop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Core is the innermost layer and must not reference WebShop.Api");
    }

    [Fact]
    public void Business_ShouldNotDependOn_InfrastructureLayer()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .ShouldNot().HaveDependencyOn("WebShop.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Business must not depend on WebShop.Infrastructure to preserve dependency inversion");
    }

    [Fact]
    public void Business_ShouldNotDependOn_ApiLayer()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .ShouldNot().HaveDependencyOn("WebShop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Business must not depend on the presentation layer WebShop.Api");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_ApiLayer()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot().HaveDependencyOn("WebShop.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Infrastructure must not depend on the presentation layer WebShop.Api");
    }

    #endregion

    #region Required inward dependencies

    [Fact]
    public void Business_ShouldDependOn_CoreLayer()
    {
        var dependentTypes = Types.InAssembly(BusinessAssembly)
            .That().HaveDependencyOn("WebShop.Core")
            .GetTypes();

        dependentTypes.Should().NotBeEmpty(
            because: "WebShop.Business services and validators should reference WebShop.Core types");
    }

    [Fact]
    public void Infrastructure_ShouldDependOn_CoreLayer()
    {
        var dependentTypes = Types.InAssembly(InfrastructureAssembly)
            .That().HaveDependencyOn("WebShop.Core")
            .GetTypes();

        dependentTypes.Should().NotBeEmpty(
            because: "WebShop.Infrastructure repositories must reference WebShop.Core entities and interfaces");
    }

    [Fact]
    public void Api_ShouldDependOn_BusinessLayer()
    {
        var dependentTypes = Types.InAssembly(ApiAssembly)
            .That().HaveDependencyOn("WebShop.Business")
            .GetTypes();

        dependentTypes.Should().NotBeEmpty(
            because: "WebShop.Api controllers must orchestrate through WebShop.Business services");
    }

    #endregion
}
