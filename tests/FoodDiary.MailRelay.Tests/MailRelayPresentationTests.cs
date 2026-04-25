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

public sealed class MailRelayPresentationTests {
    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.ExternalFailure, StatusCodes.Status502BadGateway)]
    [InlineData(ErrorKind.Internal, StatusCodes.Status500InternalServerError)]
    public void ErrorResult_MapsErrorKindToHttpStatus(ErrorKind kind, int expectedStatusCode) {
        var result = MailRelayResultExtensions.ErrorResult(
            new MailRelayError("code", "message", kind),
            "trace");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var response = Assert.IsType<MailRelayApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal("code", response.Error);
        Assert.Equal("trace", response.TraceId);
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
        var context = CreateAuthorizationContext();

        filter.OnAuthorization(context);

        var result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        var response = Assert.IsType<MailRelayApiErrorHttpResponse>(result.Value);
        Assert.Equal("MailRelay.Unauthorized", response.Error);
    }

    [Fact]
    public void RelayApiKeyAuthorizationFilter_WhenApiKeyMatches_AllowsRequest() {
        var filter = new RelayApiKeyAuthorizationFilter(Options.Create(new MailRelayOptions {
            RequireApiKey = true,
            ApiKey = "secret"
        }));
        var context = CreateAuthorizationContext();
        context.HttpContext.Request.Headers["X-Relay-Api-Key"] = "secret";

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task TelemetryActionFilter_ExecutesNextDelegate() {
        var filter = new MailRelayTelemetryActionFilter(NullLogger<MailRelayTelemetryActionFilter>.Instance);
        var context = CreateActionExecutingContext(new MailRelayQueueController(null!));
        var executed = false;

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
            new Dictionary<string, object?>(),
            controller);
}
