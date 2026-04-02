using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using WebShop.Business;
using WebShop.Core.Entities;
using Xunit;

namespace WebShop.ArchitectureTests;

/// <summary>
/// Guards inner layers from taking dependencies on infrastructure-specific libraries.
///
/// Core and Business layers must remain decoupled from:
///   - Dapper                         (ORM - belongs only in Infrastructure)
///   - Npgsql                         (PostgreSQL driver - belongs only in Infrastructure)
///   - DbUp                           (Database migrations - belongs only in Infrastructure)
///   - StackExchange.Redis            (Cache provider - belongs only in Infrastructure)
///
/// Leaking these libraries into inner layers tightly couples business logic to
/// persistence and caching technology choices, making them hard to test and replace.
/// </summary>
public class DependencyGuardTests
{
    private static readonly Assembly CoreAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly BusinessAssembly = typeof(DependencyInjection).Assembly;

    #region Dapper (ORM)

    [Fact]
    public void CoreLayer_ShouldNotDependOn_Dapper()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Core must remain persistence-ignorant - Dapper belongs only in WebShop.Infrastructure");
    }

    [Fact]
    public void BusinessLayer_ShouldNotDependOn_Dapper()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .ShouldNot().HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Business must not depend on Dapper; data access concerns belong in repositories within WebShop.Infrastructure");
    }

    #endregion

    #region Npgsql (PostgreSQL driver)

    [Fact]
    public void CoreLayer_ShouldNotDependOn_Npgsql()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Core must be database-agnostic - Npgsql is a PostgreSQL-specific driver that belongs only in WebShop.Infrastructure");
    }

    [Fact]
    public void BusinessLayer_ShouldNotDependOn_Npgsql()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .ShouldNot().HaveDependencyOn("Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Business must be database-agnostic - Npgsql is a PostgreSQL-specific driver that belongs only in WebShop.Infrastructure");
    }

    #endregion

    #region DbUp (Database migrations)

    [Fact]
    public void CoreLayer_ShouldNotDependOn_DbUp()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("DbUp")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Core must not reference migration tooling - DbUp belongs only in WebShop.Infrastructure");
    }

    [Fact]
    public void BusinessLayer_ShouldNotDependOn_DbUp()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .ShouldNot().HaveDependencyOn("DbUp")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Business must not reference migration tooling - DbUp belongs only in WebShop.Infrastructure");
    }

    #endregion

    #region StackExchange.Redis (Cache provider)

    [Fact]
    public void CoreLayer_ShouldNotDependOn_StackExchangeRedis()
    {
        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("StackExchange.Redis")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Core must be cache-provider-agnostic - Redis implementation belongs only in WebShop.Infrastructure");
    }

    [Fact]
    public void BusinessLayer_ShouldNotDependOn_StackExchangeRedis()
    {
        var result = Types.InAssembly(BusinessAssembly)
            .ShouldNot().HaveDependencyOn("StackExchange.Redis")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "WebShop.Business must be cache-provider-agnostic - Redis implementation belongs only in WebShop.Infrastructure");
    }

    #endregion
}
