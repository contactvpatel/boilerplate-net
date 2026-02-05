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
/// Unit tests for AsmService.
/// </summary>
[Trait("Category", "Unit")]
public class AsmServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IOptions<AsmServiceOptions>> _mockOptions;
    private readonly Mock<ILogger<AsmService>> _mockLogger;
    private readonly Mock<IOptions<HttpResilienceOptions>> _mockResilienceOptions;
    private readonly AsmService _service;

    public AsmServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockOptions = new Mock<IOptions<AsmServiceOptions>>();
        _mockLogger = new Mock<ILogger<AsmService>>();
        _mockResilienceOptions = new Mock<IOptions<HttpResilienceOptions>>();

        _mockOptions.Setup(o => o.Value).Returns(new AsmServiceOptions
        {
            Endpoint = new AsmServiceEndpoints
            {
                ApplicationSecurity = "/api/applicationsecurity"
            }
        });

        _mockResilienceOptions.Setup(o => o.Value).Returns(new HttpResilienceOptions
        {
            MaxRequestSizeBytes = 1024 * 1024,
            MaxResponseSizeBytes = 10 * 1024 * 1024
        });

        _service = new AsmService(
            _mockHttpClientFactory.Object,
            _mockOptions.Object,
            _mockLogger.Object,
            _mockResilienceOptions.Object);
    }

    #region GetApplicationSecurityAsync Tests

    [Fact]
    public async Task GetApplicationSecurityAsync_ValidPersonId_ReturnsSecurityInfo()
    {
        // Arrange - API returns AsmApiResponse with { data, succeeded }
        const string personId = "person-123";
        const string token = "bearer-token";
        AsmApiResponse apiResponse = new AsmApiResponse
        {
            Data =
            [
                new AsmResponseModel
                {
                    PositionId = 111,
                    RoleId = 111,
                    ApplicationAccess =
                    [
                        new ApplicationAccessModel { ModuleCode = "app-1", ModuleName = "Test App", HasViewAccess = true }
                    ]
                }
            ],
            Succeeded = true
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, apiResponse);
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://asm.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("AsmService")).Returns(httpClient);

        // Act - Infrastructure returns list of AsmResponseModel (parsed from API wrapper)
        IReadOnlyList<AsmResponseModel> result = await _service.GetApplicationSecurityAsync(personId, token);

        // Assert
        result.Should().NotBeNull().And.HaveCount(1);
        result[0].ApplicationAccess.Should().HaveCount(1);
        result[0].ApplicationAccess[0].ModuleCode.Should().Be("app-1");
        result[0].ApplicationAccess[0].ModuleName.Should().Be("Test App");
        VerifyRequest(mockHandler, HttpMethod.Get, "/api/applicationsecurity/");
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_NoSecurity_ReturnsEmptyList()
    {
        // Arrange - API returns { data: [], succeeded: true }
        const string personId = "person-999";
        const string token = "bearer-token";
        AsmApiResponse apiResponse = new AsmApiResponse { Data = [], Succeeded = true };
        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, apiResponse);
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://asm.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("AsmService")).Returns(httpClient);

        // Act
        IReadOnlyList<AsmResponseModel> result = await _service.GetApplicationSecurityAsync(personId, token);

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region Helper Methods

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
