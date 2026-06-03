using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class ExtensionsTests {
    [Fact]
    public void ResultExtensions_Success_ReturnsOkObjectResult() {
        var result = Result.Success("ok");

        var actionResult = result.ToActionResult();

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("ok", ok.Value);
    }

    [Fact]
    public void ResultExtensions_AuthenticationError_ReturnsUnauthorized() {
        var result = Result.Failure<string>(CreateError("Authentication.InvalidToken", "Invalid authorization token."));

        var actionResult = result.ToActionResult();

        var unauthorized = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public void ResultExtensions_ValidationError_ReturnsBadRequest() {
        var result = Result.Failure<string>(CreateError("Validation.Invalid", "Invalid field."));

        var actionResult = result.ToActionResult();

        var badRequest = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void ResultExtensions_ValidationConflict_ReturnsConflict() {
        var result = Result.Failure<string>(CreateError("Validation.Conflict", "Conflict."));

        var actionResult = result.ToActionResult();

        var conflict = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public void ResultExtensions_NotFoundError_ReturnsNotFound() {
        var result = Result.Failure<string>(CreateError("User.NotFound", "Not found."));

        var actionResult = result.ToActionResult();

        var notFound = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void ResultExtensions_NotAccessibleError_ReturnsNotFound() {
        var result = Result.Failure<string>(CreateError("Product.NotAccessible", "Not accessible."));

        var actionResult = result.ToActionResult();

        var notFound = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void ResultExtensions_AlreadyExistsError_ReturnsConflict() {
        var result = Result.Failure<string>(CreateError("Product.AlreadyExists", "Already exists."));

        var actionResult = result.ToActionResult();

        var conflict = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public void ResultExtensions_AiQuotaExceeded_ReturnsTooManyRequests() {
        var result = Result.Failure<string>(CreateError("Ai.QuotaExceeded", "Quota exceeded."));

        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
    }

    [Fact]
    public void ResultExtensions_UnknownError_ReturnsInternalServerError() {
        var result = Result.Failure<string>(CreateError("Something.Unmapped", "Unexpected."));

        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public void ResultExtensions_ErrorResponse_ContainsCurrentActivityTraceId() {
        using var activity = new Activity("result-extension-test");
        activity.Start();
        var result = Result.Failure<string>(CreateError("Validation.Invalid", "Invalid field."));

        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        var response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(activity.Id, response.TraceId);
    }

    [Fact]
    public void UserExtensions_WithValidUserIdClaim_ReturnsUserId() {
        var expectedGuid = Guid.NewGuid();
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expectedGuid.ToString())], "test"));

        var userId = user.GetUserGuid();

        Assert.NotNull(userId);
        Assert.Equal(expectedGuid, userId.Value);
    }

    [Fact]
    public void UserExtensions_WithGuidEmptyClaim_ReturnsNull() {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString())], "test"));

        var userId = user.GetUserGuid();

        Assert.Null(userId);
    }

    [Fact]
    public void UserExtensions_WithInvalidClaim_ReturnsNull() {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")], "test"));

        var userId = user.GetUserGuid();

        Assert.Null(userId);
    }

    [Fact]
    public void ApiPipelineHealthCheckPredicates_SelectExpectedRegistrations() {
        var excludeHealthChecks = typeof(ApiApplicationBuilderExtensions).GetMethod(
            "ExcludeHealthChecks",
            BindingFlags.NonPublic | BindingFlags.Static);
        var isReadyHealthCheck = typeof(ApiApplicationBuilderExtensions).GetMethod(
            "IsReadyHealthCheck",
            BindingFlags.NonPublic | BindingFlags.Static);
        var readyRegistration = CreateHealthCheckRegistration("postgresql", ["ready"]);
        var liveOnlyRegistration = CreateHealthCheckRegistration("self", []);

        var liveIncludesReady = (bool)excludeHealthChecks!.Invoke(null, [readyRegistration])!;
        var readyIncludesReady = (bool)isReadyHealthCheck!.Invoke(null, [readyRegistration])!;
        var readyIncludesLiveOnly = (bool)isReadyHealthCheck.Invoke(null, [liveOnlyRegistration])!;

        Assert.False(liveIncludesReady);
        Assert.True(readyIncludesReady);
        Assert.False(readyIncludesLiveOnly);
    }

    [Fact]
    public void UseApiPipeline_InProduction_ConfiguresHostPipeline() {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            EnvironmentName = Environments.Production,
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
            ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
            ["Jwt:Issuer"] = "FoodDiaryApi",
            ["Jwt:Audience"] = "FoodDiaryClient",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["TelegramBot:ApiSecret"] = "",
            ["Cors:Origins:0"] = "http://localhost:4200",
        });
        builder.Services.AddApiServices(builder.Configuration);
        using var app = builder.Build();

        var configured = app.UseApiPipeline();

        Assert.Same(app, configured);
    }

    private static Error CreateError(string errorCode, string message) =>
        new(errorCode, message, kind: ErrorKindResolver.Resolve(errorCode));

    private static HealthCheckRegistration CreateHealthCheckRegistration(string name, IReadOnlyCollection<string> tags) =>
        new(
            name,
            new HealthyCheck(),
            failureStatus: null,
            tags);

    private sealed class HealthyCheck : IHealthCheck {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }
}
