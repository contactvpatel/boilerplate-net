using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WebShop.Core.Models;
using WebShop.Infrastructure.Services.External;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.Infrastructure.Tests.Services.External;

/// <summary>
/// Unit tests for MisService.
/// </summary>
[Trait("Category", "Unit")]
public class MisServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IOptions<MisServiceOptions>> _mockOptions;
    private readonly Mock<ILogger<MisService>> _mockLogger;
    private readonly Mock<IOptions<HttpResilienceOptions>> _mockResilienceOptions;
    private readonly MisService _service;

    public MisServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockOptions = new Mock<IOptions<MisServiceOptions>>();
        _mockLogger = new Mock<ILogger<MisService>>();
        _mockResilienceOptions = new Mock<IOptions<HttpResilienceOptions>>();

        _mockOptions.Setup(o => o.Value).Returns(new MisServiceOptions
        {
            Endpoint = new MisServiceEndpoints
            {
                Department = "/api/departments",
                RoleType = "/api/roletypes",
                Role = "/api/roles",
                Position = "/api/positions",
                PersonPosition = "/api/personpositions"
            }
        });

        _mockResilienceOptions.Setup(o => o.Value).Returns(new HttpResilienceOptions
        {
            MaxRequestSizeBytes = 1024 * 1024,
            MaxResponseSizeBytes = 10 * 1024 * 1024
        });

        _service = new MisService(
            _mockHttpClientFactory.Object,
            _mockOptions.Object,
            _mockLogger.Object,
            _mockResilienceOptions.Object);
    }

    #region GetAllDepartmentsAsync Tests

    [Fact]
    public async Task GetAllDepartmentsAsync_ValidDivisionId_ReturnsDepartments()
    {
        // Arrange
        const int divisionId = 1;
        List<DepartmentModel> expectedDepartments = new()
        {
            new DepartmentModel { Id = 1, Name = "IT", DivisionId = divisionId },
            new DepartmentModel { Id = 2, Name = "HR", DivisionId = divisionId }
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponse(expectedDepartments));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        IReadOnlyList<DepartmentModel> result = await _service.GetAllDepartmentsAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        VerifyRequest(mockHandler, HttpMethod.Get, $"/api/departments?divisionId={divisionId}");
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_NoDepartments_ReturnsEmptyList()
    {
        // Arrange
        const int divisionId = 999;
        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponse(new List<DepartmentModel>()));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        IReadOnlyList<DepartmentModel> result = await _service.GetAllDepartmentsAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetDepartmentByIdAsync Tests

    [Fact]
    public async Task GetDepartmentByIdAsync_ValidId_ReturnsDepartment()
    {
        // Arrange
        const int departmentId = 1;
        DepartmentModel expectedDepartment = new() { Id = departmentId, Name = "IT", DivisionId = 1 };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponseSingle(expectedDepartment));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        DepartmentModel? result = await _service.GetDepartmentByIdAsync(departmentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(departmentId);
        VerifyRequest(mockHandler, HttpMethod.Get, $"/api/departments/{departmentId}");
    }

    [Fact]
    public async Task GetDepartmentByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int departmentId = 999;
        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.NotFound, new { });
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        DepartmentModel? result = await _service.GetDepartmentByIdAsync(departmentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllRoleTypesAsync Tests

    [Fact]
    public async Task GetAllRoleTypesAsync_ValidDivisionId_ReturnsRoleTypes()
    {
        // Arrange
        const int divisionId = 1;
        List<RoleTypeModel> expectedRoleTypes = new()
        {
            new RoleTypeModel { Id = 1, Name = "Manager", DivisionId = divisionId },
            new RoleTypeModel { Id = 2, Name = "Employee", DivisionId = divisionId }
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponse(expectedRoleTypes));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        IReadOnlyList<RoleTypeModel> result = await _service.GetAllRoleTypesAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetAllRolesAsync Tests

    [Fact]
    public async Task GetAllRolesAsync_ValidDivisionId_ReturnsRoles()
    {
        // Arrange
        const int divisionId = 1;
        List<RoleModel> expectedRoles = new()
        {
            new RoleModel { Id = 1, Name = "Admin", DivisionId = divisionId },
            new RoleModel { Id = 2, Name = "User", DivisionId = divisionId }
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponse(expectedRoles));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        IReadOnlyList<RoleModel> result = await _service.GetAllRolesAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetRoleByIdAsync Tests

    [Fact]
    public async Task GetRoleByIdAsync_ValidId_ReturnsRole()
    {
        // Arrange
        const int roleId = 1;
        RoleModel expectedRole = new() { Id = roleId, Name = "Admin", DivisionId = 1 };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponseSingle(expectedRole));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        RoleModel? result = await _service.GetRoleByIdAsync(roleId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(roleId);
    }

    #endregion

    #region GetRolesByDepartmentIdAsync Tests

    [Fact]
    public async Task GetRolesByDepartmentIdAsync_ValidDepartmentId_ReturnsRoles()
    {
        // Arrange
        const int departmentId = 1;
        List<RoleModel> expectedRoles = new()
        {
            new RoleModel { Id = 1, Name = "Admin", DepartmentId = departmentId }
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponse(expectedRoles));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        IReadOnlyList<RoleModel> result = await _service.GetRolesByDepartmentIdAsync(departmentId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetPositionsByRoleIdAsync Tests

    [Fact]
    public async Task GetPositionsByRoleIdAsync_ValidRoleId_ReturnsPositions()
    {
        // Arrange
        const int roleId = 1;
        List<PositionModel> expectedPositions = new()
        {
            new PositionModel { Id = 1, Name = "Senior Developer", RoleId = roleId }
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponse(expectedPositions));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        IReadOnlyList<PositionModel> result = await _service.GetPositionsByRoleIdAsync(roleId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetPersonPositionsAsync Tests

    [Fact]
    public async Task GetPersonPositionsAsync_ValidPersonId_ReturnsPositions()
    {
        // Arrange
        const string personId = "person-123";
        List<PersonPositionModel> expectedPositions = new()
        {
            new PersonPositionModel { PersonId = personId, PositionId = 1 }
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, OkResponse(expectedPositions));
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://mis.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("MisService")).Returns(httpClient);

        // Act
        IReadOnlyList<PersonPositionModel> result = await _service.GetPersonPositionsAsync(personId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    /// <summary>Wraps list data in the MIS API response shape so the service can deserialize MisResponse{T}.</summary>
    private static MisResponse<T> OkResponse<T>(List<T> data)
    {
        return new() { Succeeded = true, Data = data };
    }

    /// <summary>Wraps a single item in the MIS API response shape (Data list with one element).</summary>
    private static MisResponse<T> OkResponseSingle<T>(T item)
    {
        return new() { Succeeded = true, Data = new List<T> { item } };
    }

    private static Mock<HttpMessageHandler> CreateMockHandler<T>(HttpStatusCode statusCode, T response)
    {
        Mock<HttpMessageHandler> mockHandler = new(MockBehavior.Strict);
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = JsonContent.Create(response)
            });
        return mockHandler;
    }

    private static void VerifyRequest(Mock<HttpMessageHandler> mockHandler, HttpMethod method, string endpoint)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.PathAndQuery.Contains(endpoint)),
                ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
