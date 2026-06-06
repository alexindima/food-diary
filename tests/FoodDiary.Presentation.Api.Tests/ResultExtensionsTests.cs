using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ResultExtensionsTests {
    [Theory]
    [InlineData("Authentication.TelegramInvalidData", StatusCodes.Status400BadRequest)]
    [InlineData("Authentication.TelegramAuthExpired", StatusCodes.Status401Unauthorized)]
    [InlineData("Authentication.TelegramNotLinked", StatusCodes.Status404NotFound)]
    [InlineData("Authentication.TelegramAlreadyLinked", StatusCodes.Status409Conflict)]
    [InlineData("Authentication.AdminSsoForbidden", StatusCodes.Status403Forbidden)]
    [InlineData("Authentication.AdminSsoInvalidCode", StatusCodes.Status401Unauthorized)]
    [InlineData("Authentication.InvalidCredentials", StatusCodes.Status401Unauthorized)]
    [InlineData("Ai.Forbidden", StatusCodes.Status403Forbidden)]
    [InlineData("Validation.Conflict", StatusCodes.Status409Conflict)]
    [InlineData("Validation.Invalid", StatusCodes.Status400BadRequest)]
    [InlineData("Product.NotAccessible", StatusCodes.Status404NotFound)]
    [InlineData("Recipe.AlreadyExists", StatusCodes.Status409Conflict)]
    [InlineData("User.NotFound", StatusCodes.Status404NotFound)]
    [InlineData("Ai.QuotaExceeded", StatusCodes.Status429TooManyRequests)]
    [InlineData("Ai.OpenAiFailed", StatusCodes.Status502BadGateway)]
    [InlineData("Ai.InvalidResponse", StatusCodes.Status502BadGateway)]
    [InlineData("Image.Forbidden", StatusCodes.Status403Forbidden)]
    [InlineData("Image.InUse", StatusCodes.Status409Conflict)]
    [InlineData("Image.StorageError", StatusCodes.Status502BadGateway)]
    [InlineData("Unhandled.Custom", StatusCodes.Status500InternalServerError)]
    public void ToActionResult_FailedResult_MapsExpectedStatusCode(string errorCode, int expectedStatusCode) {
        var result = Result.Failure(CreateError(errorCode, "Failure"));

        IActionResult actionResult = result.ToActionResult();

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        Assert.Equal(errorCode, response.Error);
    }

    [Fact]
    public void ToActionResult_SuccessfulResult_ReturnsOk() {
        var result = Result.Success();

        IActionResult actionResult = result.ToActionResult();

        Assert.IsType<OkResult>(actionResult);
    }

    [Fact]
    public void ToOkActionResult_FailedGenericResult_ReturnsStandardApiErrorResponse() {
        var result = Result.Failure<string>(CreateError("Image.StorageError", "Storage failed"));
        TestController controller = CreateController("trace-ok-failure");

        IActionResult actionResult = result.ToOkActionResult(controller);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        Assert.Equal("Image.StorageError", response.Error);
        Assert.Equal("trace-ok-failure", response.TraceId);
    }

    [Fact]
    public void ToActionResult_SuccessfulGenericResult_ReturnsOkObjectResult() {
        var result = Result.Success("value");

        IActionResult actionResult = result.ToActionResult();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("value", okResult.Value);
    }

    [Fact]
    public void ToOkActionResult_WithMap_MapsSuccessfulValue() {
        var result = Result.Success("value");
        TestController controller = CreateController("trace-success");

        IActionResult actionResult = result.ToOkActionResult(controller, value => value.ToUpperInvariant());

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("VALUE", okResult.Value);
    }

    [Fact]
    public void ToNoContentActionResult_SuccessfulResult_ReturnsNoContent() {
        var result = Result.Success();
        TestController controller = CreateController("trace-no-content");

        IActionResult actionResult = result.ToNoContentActionResult(controller);

        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public void ToNoContentActionResult_FailedResult_UsesControllerTraceIdentifier() {
        var result = Result.Failure(CreateError("Validation.Invalid", "Failure"));
        TestController controller = CreateController("trace-no-content-failure");

        IActionResult actionResult = result.ToNoContentActionResult(controller);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal("trace-no-content-failure", response.TraceId);
    }

    [Fact]
    public void ToErrorActionResult_UsesExplicitStatusCode() {
        var error = new Error("Custom.Error", "Failure");

        IActionResult actionResult = error.ToErrorActionResult(StatusCodes.Status418ImATeapot);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status418ImATeapot, objectResult.StatusCode);
        Assert.Equal("Custom.Error", response.Error);
    }

    [Fact]
    public void ToActionResult_ValidationError_MapsStructuredErrors() {
        var error = new Error(
            "Validation.Invalid",
            "Invalid email format",
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Email"] = ["Invalid email format"],
            },
            ErrorKindResolver.Resolve("Validation.Invalid"));
        var result = Result.Failure(error);

        IActionResult actionResult = result.ToActionResult();

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.NotNull(response.Errors);
        Assert.True(response.Errors.TryGetValue("email", out string[]? errors));
        Assert.Equal(["Invalid email format"], errors);
    }

    private static Error CreateError(string errorCode, string message) =>
        new(errorCode, message, kind: ErrorKindResolver.Resolve(errorCode));

    private static TestController CreateController(string traceIdentifier) =>
        new() {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = traceIdentifier,
                },
            },
        };

    [ExcludeFromCodeCoverage]
    private sealed class TestController : ControllerBase;
}
