using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;
using WebShop.Api.Controllers;
using WebShop.Business;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Extensions;
using Xunit;

namespace WebShop.ArchitectureTests;

/// <summary>
/// Enforces structural rules: correct inheritance, interface implementations, and encapsulation.
/// These tests ensure types follow expected patterns and maintain proper class hierarchies.
/// </summary>
public class StructureTests
{
    private static readonly Assembly CoreAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly BusinessAssembly = typeof(DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(DataAccessExtensions).Assembly;
    private static readonly Assembly ApiAssembly = typeof(BaseApiController).Assembly;

    #region Interface visibility

    [Fact]
    public void CoreInterfaces_ShouldBePublic()
    {
        var result = Types.InAssembly(CoreAssembly)
            .That().AreInterfaces()
            .Should().BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "core interfaces define contracts consumed by other layers and must be publicly accessible");
    }

    [Fact]
    public void BusinessInterfaces_ShouldBePublic()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .That().AreInterfaces()
            .Should().BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "business service interfaces are injected by the API layer and must be publicly accessible");
    }

    #endregion

    #region Repository structure

    [Fact]
    public void InfrastructureRepositories_ShouldDependOn_CoreInterfaces()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().ResideInNamespace("WebShop.Infrastructure.Repositories")
            .And().AreClasses()
            .And().ArePublic()
            .And().AreNotAbstract()
            .Should().HaveDependencyOn("WebShop.Core")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "repositories in WebShop.Infrastructure must depend on WebShop.Core contracts they implement");
    }

    [Fact]
    public void InfrastructureRepositories_ShouldBeSealed()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().ResideInNamespace("WebShop.Infrastructure.Repositories")
            .And().AreClasses()
            .And().AreNotAbstract()
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "repository implementations are concrete leaf classes that should not be subclassed");
    }

    #endregion

    #region Entity inheritance

    [Fact]
    public void CoreEntities_ShouldInheritFrom_BaseEntity()
    {
        var allEntityTypes = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespace("WebShop.Core.Entities")
            .And().AreClasses()
            .And().AreNotAbstract()
            .GetTypes();

        var entitiesInheritingBase = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespace("WebShop.Core.Entities")
            .And().AreClasses()
            .And().AreNotAbstract()
            .And().Inherit(typeof(BaseEntity))
            .GetTypes();

        var coveredTypeNames = entitiesInheritingBase
            .Select(t => t.FullName)
            .ToHashSet();

        allEntityTypes.Should().AllSatisfy(t =>
            coveredTypeNames.Should().Contain(t.FullName,
                because: $"{t.Name} must inherit from BaseEntity"));
    }

    #endregion

    #region Validator inheritance

    [Fact]
    public void BusinessValidators_ShouldInheritFrom_AbstractValidator()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .That().ResideInNamespace("WebShop.Business.Validators")
            .And().AreClasses()
            .Should().Inherit(typeof(FluentValidation.AbstractValidator<>))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all validators in WebShop.Business.Validators must extend FluentValidation.AbstractValidator<T>");
    }

    #endregion

    #region Controller inheritance

    [Fact]
    public void ApiControllers_ShouldInheritFrom_ControllerBase()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("WebShop.Api.Controllers")
            .And().AreClasses()
            .And().AreNotAbstract()
            .Should().Inherit(typeof(ControllerBase))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all concrete controllers must inherit from ControllerBase (via BaseApiController)");
    }

    #endregion

    #region External service structure

    [Fact]
    public void InfrastructureExternalServices_ShouldDependOn_CoreInterfaces()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().ResideInNamespace("WebShop.Infrastructure.Services.External")
            .And().AreClasses()
            .Should().HaveDependencyOn("WebShop.Core")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "external service implementations in WebShop.Infrastructure must implement interfaces defined in WebShop.Core");
    }

    #endregion

    #region Controller dependency scope

    [Fact]
    public void ApiControllers_ShouldNotDependOn_InfrastructureRepositories()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace("WebShop.Api.Controllers")
            .And().AreClasses()
            .ShouldNot().HaveDependencyOn("WebShop.Infrastructure.Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "controllers must not bypass the Business layer by accessing repositories directly");
    }

    #endregion

    #region Domain layer purity

    [Fact]
    public void CoreConcreteTypes_ShouldResideIn_CoreNamespace()
    {
        var result = Types.InAssembly(CoreAssembly)
            .That().AreClasses()
            .And().AreNotAbstract()
            .Should().ResideInNamespace("WebShop.Core")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "all concrete types in WebShop.Core must live within the WebShop.Core namespace tree");
    }

    #endregion
}
