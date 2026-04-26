using System.Diagnostics;
using System.Security.Claims;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    private static Error CreateError(string errorCode, string message) =>
        new(errorCode, message, kind: ErrorKindResolver.Resolve(errorCode));
}
