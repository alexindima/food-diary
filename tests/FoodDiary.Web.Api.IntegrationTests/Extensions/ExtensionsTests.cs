using System.Security.Claims;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Extensions;
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
        var result = Result.Failure<string>(new Error("Authentication.InvalidToken", "Invalid authorization token."));

        var actionResult = result.ToActionResult();

        var unauthorized = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public void ResultExtensions_ValidationError_ReturnsBadRequest() {
        var result = Result.Failure<string>(new Error("Validation.Invalid", "Invalid field."));

        var actionResult = result.ToActionResult();

        var badRequest = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void ResultExtensions_NotFoundError_ReturnsNotFound() {
        var result = Result.Failure<string>(new Error("User.NotFound", "Not found."));

        var actionResult = result.ToActionResult();

        var notFound = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void ResultExtensions_AlreadyExistsError_ReturnsConflict() {
        var result = Result.Failure<string>(new Error("Product.AlreadyExists", "Already exists."));

        var actionResult = result.ToActionResult();

        var conflict = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public void ResultExtensions_AiQuotaExceeded_ReturnsTooManyRequests() {
        var result = Result.Failure<string>(new Error("Ai.QuotaExceeded", "Quota exceeded."));

        var actionResult = result.ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
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
}
