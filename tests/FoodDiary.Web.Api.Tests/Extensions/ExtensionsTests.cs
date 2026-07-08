using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using FoodDiary.Results;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Telemetry;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ExtensionsTests {
    [Fact]
    public void ResultExtensions_Success_ReturnsOkObjectResult() {
        var result = Result.Success("ok");

        IActionResult actionResult = result.ToActionResult();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("ok", ok.Value);
    }

    [Fact]
    public void ResultExtensions_AuthenticationError_ReturnsUnauthorized() {
        var result = Result.Failure<string>(CreateError("Authentication.InvalidToken", "Invalid authorization token."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult unauthorized = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public void ResultExtensions_ValidationError_ReturnsBadRequest() {
        var result = Result.Failure<string>(CreateError("Validation.Invalid", "Invalid field."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult badRequest = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void ResultExtensions_ValidationConflict_ReturnsConflict() {
        var result = Result.Failure<string>(CreateError("Validation.Conflict", "Conflict."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult conflict = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public void ResultExtensions_NotFoundError_ReturnsNotFound() {
        var result = Result.Failure<string>(CreateError("User.NotFound", "Not found."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult notFound = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void ResultExtensions_NotAccessibleError_ReturnsNotFound() {
        var result = Result.Failure<string>(CreateError("Product.NotAccessible", "Not accessible."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult notFound = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void ResultExtensions_AlreadyExistsError_ReturnsConflict() {
        var result = Result.Failure<string>(CreateError("Product.AlreadyExists", "Already exists."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult conflict = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public void ResultExtensions_AiQuotaExceeded_ReturnsTooManyRequests() {
        var result = Result.Failure<string>(CreateError("Ai.QuotaExceeded", "Quota exceeded."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
    }

    [Fact]
    public void ResultExtensions_UnknownError_ReturnsInternalServerError() {
        var result = Result.Failure<string>(CreateError("Something.Unmapped", "Unexpected."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public void ResultExtensions_ErrorResponse_ContainsCurrentActivityTraceId() {
        using var activity = new Activity("result-extension-test");
        activity.Start();
        var result = Result.Failure<string>(CreateError("Validation.Invalid", "Invalid field."));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(activity.Id, response.TraceId);
    }

    [Fact]
    public void UserExtensions_WithValidUserIdClaim_ReturnsUserId() {
        var expectedGuid = Guid.NewGuid();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expectedGuid.ToString())], "test"));

        Guid? userId = user.GetUserGuid();

        Assert.NotNull(userId);
        Assert.Equal(expectedGuid, userId.Value);
    }

    [Fact]
    public void UserExtensions_WithGuidEmptyClaim_ReturnsNull() {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString())], "test"));

        Guid? userId = user.GetUserGuid();

        Assert.Null(userId);
    }

    [Fact]
    public void UserExtensions_WithInvalidClaim_ReturnsNull() {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")], "test"));

        Guid? userId = user.GetUserGuid();

        Assert.Null(userId);
    }

    [Fact]
    public void ApiPipelineHealthCheckPredicates_SelectExpectedRegistrations() {
        MethodInfo? excludeHealthChecks = typeof(ApiApplicationBuilderExtensions).GetMethod(
            "ExcludeHealthChecks",
            BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo? isReadyHealthCheck = typeof(ApiApplicationBuilderExtensions).GetMethod(
            "IsReadyHealthCheck",
            BindingFlags.NonPublic | BindingFlags.Static);
        HealthCheckRegistration readyRegistration = CreateHealthCheckRegistration("postgresql", ["ready"]);
        HealthCheckRegistration liveOnlyRegistration = CreateHealthCheckRegistration("self", []);

        bool liveIncludesReady = (bool)excludeHealthChecks!.Invoke(null, [readyRegistration])!;
        bool readyIncludesReady = (bool)isReadyHealthCheck!.Invoke(null, [readyRegistration])!;
        bool readyIncludesLiveOnly = (bool)isReadyHealthCheck.Invoke(null, [liveOnlyRegistration])!;

        Assert.False(liveIncludesReady);
        Assert.True(readyIncludesReady);
        Assert.False(readyIncludesLiveOnly);
    }

    [Fact]
    public void UseApiPipeline_InProduction_ConfiguresHostPipeline() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            EnvironmentName = Environments.Production,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
            ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
            ["Jwt:Issuer"] = "FoodDiaryApi",
            ["Jwt:Audience"] = "FoodDiaryClient",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["HttpsRedirection:Enabled"] = "true",
            ["TelegramBot:ApiSecret"] = "",
            ["Cors:Origins:0"] = "http://localhost:4200",
        });
        builder.Services.AddApiServices(builder.Configuration);
        using WebApplication app = builder.Build();

        WebApplication configured = app.UseApiPipeline();

        Assert.Same(app, configured);
    }

    [Fact]
    public void AddApiServices_WithRedisConnection_RegistersRedisDistributedCache() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateApiConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:Redis"] = "localhost:6379",
        });

        services.AddApiServices(configuration);

        ServiceDescriptor cacheDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IDistributedCache));
        Assert.Contains("RedisCache", cacheDescriptor.ImplementationType?.Name, StringComparison.Ordinal);
        using ServiceProvider provider = services.BuildServiceProvider();
        RedisCacheOptions options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        Assert.Equal("localhost:6379", options.Configuration);
        Assert.Equal("fooddiary:", options.InstanceName);
    }

    [Fact]
    public async Task AddApiServices_WithRedisConnection_ConfiguresRedisConnectionFactory() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateApiConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:Redis"] = "localhost:0,connectTimeout=1,abortConnect=true",
        });

        services.AddApiServices(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        RedisCacheOptions options = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

        Assert.NotNull(options.ConnectionMultiplexerFactory);
        await Assert.ThrowsAsync<RedisConnectionException>(() => options.ConnectionMultiplexerFactory());
    }

    [Fact]
    public void AddApiServices_WithoutRedisConnectionOutsideDevelopment_Throws() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateApiConfiguration([]);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddApiServices(configuration, new StubHostEnvironment(Environments.Production)));

        Assert.Equal("ConnectionStrings:Redis is required outside Development.", ex.Message);
    }

    [Fact]
    public void UseApiPipeline_OperationalEndpointsSuppressSuccessfulAccessLogs() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            EnvironmentName = Environments.Production,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
            ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
            ["Jwt:Issuer"] = "FoodDiaryApi",
            ["Jwt:Audience"] = "FoodDiaryClient",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["TelegramBot:ApiSecret"] = "",
            ["Cors:Origins:0"] = "http://localhost:4200",
        });
        builder.Services.AddApiServices(builder.Configuration);
        using WebApplication app = builder.Build();

        app.UseApiPipeline();

        foreach (string route in new[] { "/health/live", "/health/ready" }) {
            RouteEndpoint endpoint = Assert.Single(
                ((IEndpointRouteBuilder)app).DataSources.SelectMany(dataSource => dataSource.Endpoints).OfType<RouteEndpoint>(),
                candidate => RouteMatches(candidate, route));
            Assert.NotNull(endpoint.Metadata.GetMetadata<SuppressRequestAccessLogAttribute>());
        }
    }

    private static bool RouteMatches(RouteEndpoint endpoint, string expectedRoute) {
        string? actualRoute = endpoint.RoutePattern.RawText;
        return string.Equals(NormalizeRoute(actualRoute), NormalizeRoute(expectedRoute), StringComparison.Ordinal);
    }

    private static string? NormalizeRoute(string? route) =>
        route is null ? null : $"/{route.TrimStart('/')}";

    private static Error CreateError(string errorCode, string message) =>
        new(errorCode, message, Kind: ErrorKindResolver.Resolve(errorCode));

    private static IConfiguration CreateApiConfiguration(Dictionary<string, string?> overrides) {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
            ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
            ["Jwt:Issuer"] = "FoodDiaryApi",
            ["Jwt:Audience"] = "FoodDiaryClient",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["TelegramBot:ApiSecret"] = "",
            ["Cors:Origins:0"] = "http://localhost:4200",
        };
        foreach ((string key, string? value) in overrides) {
            values[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static HealthCheckRegistration CreateHealthCheckRegistration(string name, IReadOnlyCollection<string> tags) =>
        new(
            name,
            new HealthyCheck(),
            failureStatus: null,
            tags);

    [ExcludeFromCodeCoverage]
    private sealed class HealthyCheck : IHealthCheck {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "FoodDiary.Web.Api.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
