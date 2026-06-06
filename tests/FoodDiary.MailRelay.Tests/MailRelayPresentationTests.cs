using System.Globalization;
using FoodDiary.MailRelay.Application.Common.Result;
using FoodDiary.MailRelay.Application.Options;
using FoodDiary.MailRelay.Presentation.Extensions;
using FoodDiary.MailRelay.Presentation.Features.Email;
using FoodDiary.MailRelay.Presentation.Filters;
using FoodDiary.MailRelay.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayPresentationTests {
    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.ExternalFailure, StatusCodes.Status502BadGateway)]
    [InlineData(ErrorKind.Internal, StatusCodes.Status500InternalServerError)]
    public void ErrorResult_MapsErrorKindToHttpStatus(ErrorKind kind, int expectedStatusCode) {
        IActionResult result = MailRelayResultExtensions.ErrorResult(
            new MailRelayError("code", "message", kind),
            "trace");

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal("code", response.Error);
        Assert.Equal("trace", response.TraceId);
    }

    [Fact]
    public void ResultExtensions_WhenResultIsSuccessful_ReturnExpectedActionResults() {
        var controller = new TestController();
        IActionResult ok = Result<int>.Success(42).ToOkActionResult(controller, static value => new { Value = value });
        IActionResult created = Result<int>.Success(42).ToCreatedActionResult(
            controller,
            static value => string.Create(CultureInfo.InvariantCulture, $"/messages/{value}"),
            static value => new { Value = value });
        IActionResult accepted = Result<int>.Success(42).ToAcceptedActionResult(
            controller,
            static value => string.Create(CultureInfo.InvariantCulture, $"/messages/{value}"),
            static value => new { Value = value });
        IActionResult noContent = Result.Success().ToNoContentActionResult(controller);
        IActionResult okObject = Result.Success().ToOkActionResult(controller, new { Status = "ok" });

        Assert.IsType<OkObjectResult>(ok);
        Assert.IsType<CreatedResult>(created);
        Assert.IsType<AcceptedResult>(accepted);
        Assert.IsType<NoContentResult>(noContent);
        Assert.IsType<OkObjectResult>(okObject);
    }

    [Fact]
    public void ResultExtensions_WhenResultFails_ReturnErrorActionResults() {
        var controller = new TestController();
        var error = new MailRelayError("code", "message", ErrorKind.Conflict);

        IActionResult ok = Result<int>.Failure(error).ToOkActionResult(controller, static value => new { Value = value });
        IActionResult created = Result<int>.Failure(error).ToCreatedActionResult(
            controller,
            static value => $"/messages/{value.ToString(CultureInfo.InvariantCulture)}",
            static value => new { Value = value });
        IActionResult accepted = Result<int>.Failure(error).ToAcceptedActionResult(
            controller,
            static value => $"/messages/{value.ToString(CultureInfo.InvariantCulture)}",
            static value => new { Value = value });
        IActionResult noContent = Result.Failure(error).ToNoContentActionResult(controller);
        IActionResult okObject = Result.Failure(error).ToOkActionResult(controller, new { Status = "ok" });

        Assert.All([ok, created, accepted, noContent, okObject], result => {
            ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        });
    }

    [Fact]
    public void ToCamelCasePath_MapsValidationPaths() {
        Assert.Equal("request.email", MailRelayApiErrorDetailsMapper.ToCamelCasePath("Request.Email"));
        Assert.Equal("request", MailRelayApiErrorDetailsMapper.ToCamelCasePath(""));
    }

    [Fact]
    public void RelayApiKeyAuthorizationFilter_WhenApiKeyIsRequiredAndMissing_ReturnsUnauthorized() {
        var filter = new RelayApiKeyAuthorizationFilter(Options.Create(new MailRelayOptions {
            RequireApiKey = true,
            ApiKey = "secret"
        }));
        AuthorizationFilterContext context = CreateAuthorizationContext();

        filter.OnAuthorization(context);

        UnauthorizedObjectResult result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(result.Value);
        Assert.Equal("MailRelay.Unauthorized", response.Error);
    }

    [Fact]
    public void RelayApiKeyAuthorizationFilter_WhenApiKeyRequirementIsDisabled_ReturnsUnauthorized() {
        var filter = new RelayApiKeyAuthorizationFilter(Options.Create(new MailRelayOptions {
            RequireApiKey = false,
            ApiKey = "secret"
        }));
        AuthorizationFilterContext context = CreateAuthorizationContext();
        context.HttpContext.Request.Headers["X-Relay-Api-Key"] = "secret";

        filter.OnAuthorization(context);

        UnauthorizedObjectResult result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(result.Value);
        Assert.Equal("MailRelay.Unauthorized", response.Error);
    }

    [Fact]
    public void RelayApiKeyAuthorizationFilter_WhenApiKeyMatches_AllowsRequest() {
        var filter = new RelayApiKeyAuthorizationFilter(Options.Create(new MailRelayOptions {
            RequireApiKey = true,
            ApiKey = "secret"
        }));
        AuthorizationFilterContext context = CreateAuthorizationContext();
        context.HttpContext.Request.Headers["X-Relay-Api-Key"] = "secret";

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task TelemetryActionFilter_ExecutesNextDelegate() {
        var filter = new MailRelayTelemetryActionFilter(NullLogger<MailRelayTelemetryActionFilter>.Instance);
        ActionExecutingContext context = CreateActionExecutingContext(new MailRelayQueueController(null!));
        bool executed = false;

        await filter.OnActionExecutionAsync(context, () => {
            executed = true;
            return Task.FromResult(new ActionExecutedContext(
                context,
                context.Filters,
                context.Controller));
        });

        Assert.True(executed);
    }

    private static AuthorizationFilterContext CreateAuthorizationContext() =>
        new(
            new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor()),
            []);

    private static ActionExecutingContext CreateActionExecutingContext(object controller) =>
        new(
            new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ControllerActionDescriptor {
                    ControllerTypeInfo = controller.GetType().GetTypeInfo(),
                    AttributeRouteInfo = new AttributeRouteInfo { Template = "api/email/queue" }
                }),
            [],
            new Dictionary<string, object?>(StringComparer.Ordinal),
            controller);

    [ExcludeFromCodeCoverage]
    private sealed class TestController : ControllerBase {
        public TestController() {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace"
                }
            };
        }
    }
}
