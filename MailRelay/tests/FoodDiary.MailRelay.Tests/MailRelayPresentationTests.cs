using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.MailRelay.Application.Common.Results;
using FoodDiary.MailRelay.Application.Options;
using FoodDiary.MailRelay.Presentation.Extensions;
using FoodDiary.MailRelay.Presentation.Features.Email;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Filters;
using FoodDiary.MailRelay.Presentation.Responses;
using FoodDiary.MailRelay.Presentation.Security;
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
        IActionResult ok = Result.Success(42).ToOkActionResult(controller, static value => new { Value = value });
        IActionResult created = Result.Success(42).ToCreatedActionResult(
            controller,
            static value => string.Create(CultureInfo.InvariantCulture, $"/messages/{value}"),
            static value => new { Value = value });
        IActionResult accepted = Result.Success(42).ToAcceptedActionResult(
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

        IActionResult ok = Result.Failure<int>(error).ToOkActionResult(controller, static value => new { Value = value });
        IActionResult created = Result.Failure<int>(error).ToCreatedActionResult(
            controller,
            static value => $"/messages/{value.ToString(CultureInfo.InvariantCulture)}",
            static value => new { Value = value });
        IActionResult accepted = Result.Failure<int>(error).ToAcceptedActionResult(
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
            ApiKey = "secret",
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
            ApiKey = "secret",
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
            ApiKey = "secret",
        }));
        AuthorizationFilterContext context = CreateAuthorizationContext();
        context.HttpContext.Request.Headers["X-Relay-Api-Key"] = "secret";

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void ProviderWebhookAuthorizer_WhenMailgunSignatureMatches_AllowsRequest() {
        ProviderWebhookAuthorizer authorizer = CreateProviderWebhookAuthorizer("mailgun-secret");
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        const string token = "token";
        string signature = CreateMailgunSignature("mailgun-secret", timestamp, token);
        var request = new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            new MailgunSignatureHttpRequest(timestamp, token, signature));

        Assert.True(authorizer.IsMailgunAuthorized(request));
    }

    [Fact]
    public void ProviderWebhookAuthorizer_WhenMailgunSignatureDoesNotMatch_RejectsRequest() {
        ProviderWebhookAuthorizer authorizer = CreateProviderWebhookAuthorizer("mailgun-secret");
        var request = new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            new MailgunSignatureHttpRequest("1710000000", "token", "invalid"));

        Assert.False(authorizer.IsMailgunAuthorized(request));
    }

    [Fact]
    public void ProviderWebhookAuthorizer_WhenMailgunTimestampIsExpired_RejectsRequest() {
        ProviderWebhookAuthorizer authorizer = CreateProviderWebhookAuthorizer("mailgun-secret");
        string timestamp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        const string token = "token";
        string signature = CreateMailgunSignature("mailgun-secret", timestamp, token);
        var request = new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            new MailgunSignatureHttpRequest(timestamp, token, signature));

        Assert.False(authorizer.IsMailgunAuthorized(request));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsCertHostIsNotTrusted_RejectsBeforeDownloadingCertificate() {
        var handler = new RecordingHttpMessageHandler();
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions()),
            new HttpClient(handler));
        var request = new AwsSesSnsWebhookHttpRequest(
            Type: "Notification",
            Message: "{}",
            MessageId: "message-id",
            TopicArn: "arn:aws:sns:us-east-1:123456789012:topic",
            Timestamp: DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            SignatureVersion: "2",
            Signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            SigningCertURL: "https://sns.extra.amazonaws.com/SimpleNotificationService-test.pem");

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(request, CancellationToken.None));
        Assert.False(handler.WasCalled);
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
                    AttributeRouteInfo = new AttributeRouteInfo { Template = "api/email/queue" },
                }),
            [],
            new Dictionary<string, object?>(StringComparer.Ordinal),
            controller);

    private static ProviderWebhookAuthorizer CreateProviderWebhookAuthorizer(string mailgunSigningKey) =>
        new(
            Options.Create(new MailRelayOptions {
                RequireMailgunWebhookSignature = true,
                MailgunWebhookSigningKey = mailgunSigningKey,
            }),
            new HttpClient());

    private static string CreateMailgunSignature(string signingKey, string timestamp, string token) {
        byte[] keyBytes = Encoding.UTF8.GetBytes(signingKey);
        byte[] valueBytes = Encoding.UTF8.GetBytes(string.Concat(timestamp, token));
        return Convert.ToHexString(HMACSHA256.HashData(keyBytes, valueBytes)).ToLowerInvariant();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHttpMessageHandler : HttpMessageHandler {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            WasCalled = true;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestController : ControllerBase {
        public TestController() {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace",
                },
            };
        }
    }
}
