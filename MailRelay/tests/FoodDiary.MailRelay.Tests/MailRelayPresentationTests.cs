using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.MailRelay.Application.Common.Results;
using FoodDiary.MailRelay.Application.Emails.Commands;
using FoodDiary.MailRelay.Application.Health;
using FoodDiary.MailRelay.Application.Options;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Presentation.Extensions;
using FoodDiary.MailRelay.Presentation.Features.Email;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using FoodDiary.MailRelay.Presentation.Features.Health;
using FoodDiary.MailRelay.Presentation.Features.Health.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Health.Responses;
using FoodDiary.MailRelay.Presentation.Filters;
using FoodDiary.MailRelay.Presentation.Responses;
using FoodDiary.MailRelay.Presentation.Security;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
    public void ProviderWebhookAuthorizer_WhenMailgunSignatureRequirementIsDisabled_AllowsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions {
                RequireMailgunWebhookSignature = false,
            }),
            new HttpClient());

        Assert.True(authorizer.IsMailgunAuthorized(new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            Signature: null)));
    }

    [Fact]
    public void ProviderWebhookAuthorizer_WhenMailgunKeyOrSignatureIsMissing_RejectsRequest() {
        var missingKey = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions {
                RequireMailgunWebhookSignature = true,
            }),
            new HttpClient());
        ProviderWebhookAuthorizer validKey = CreateProviderWebhookAuthorizer("mailgun-secret");

        Assert.False(missingKey.IsMailgunAuthorized(new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            new MailgunSignatureHttpRequest("1", "token", "signature"))));
        Assert.False(validKey.IsMailgunAuthorized(new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            Signature: null)));
    }

    [Theory]
    [InlineData("not-a-timestamp")]
    [InlineData("999999999999999999999999")]
    public void ProviderWebhookAuthorizer_WhenMailgunTimestampIsInvalid_RejectsRequest(string timestamp) {
        ProviderWebhookAuthorizer authorizer = CreateProviderWebhookAuthorizer("mailgun-secret");

        Assert.False(authorizer.IsMailgunAuthorized(new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            new MailgunSignatureHttpRequest(timestamp, "token", "signature"))));
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
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsSignatureRequirementIsDisabled_AllowsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions {
                RequireAwsSesSnsSignature = false,
            }),
            new HttpClient());

        Assert.True(await authorizer.IsAwsSesSnsAuthorizedAsync(new AwsSesSnsWebhookHttpRequest(
            Type: "",
            Message: null), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsRequiredFieldsAreMissing_RejectsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler()));

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(new AwsSesSnsWebhookHttpRequest(
            Type: "Notification",
            Message: "{}"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsSignatureIsNotBase64_RejectsRequest() {
        var handler = new RecordingHttpMessageHandler();
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions()),
            new HttpClient(handler));

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: "not-base64",
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsCertificateDownloadFails_RejectsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => throw new HttpRequestException("download failed"))));

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsCertificatePemIsInvalid_RejectsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent("not a pem"),
            })));

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsConfirmationMessageHasInvalidPem_BuildsConfirmationCanonicalString() {
        var authorizer = new ProviderWebhookAuthorizer(
            Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent("not a pem"),
            })));

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.amazonaws.com/SimpleNotificationService-test.pem",
            type: "SubscriptionConfirmation"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderEventsController_WhenMailgunSignatureIsInvalid_ReturnsUnauthorized() {
        MailRelayProviderEventsController controller = CreateProviderEventsController(
            new RecordingSender(),
            new ProviderWebhookAuthorizer(
                Options.Create(new MailRelayOptions {
                    RequireMailgunWebhookSignature = true,
                    MailgunWebhookSigningKey = "mailgun-secret",
                }),
                new HttpClient()));

        IActionResult result = await controller.IngestMailgun(new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            Signature: null));

        UnauthorizedObjectResult unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(unauthorized.Value);
        Assert.Equal("MailRelay.ProviderWebhook.Unauthorized", response.Error);
    }

    [Fact]
    public async Task ProviderEventsController_WhenMailgunEventIsAccepted_ReturnsCreatedResponse() {
        MailRelayDeliveryEventEntry entry = CreateDeliveryEvent();
        var sender = new RecordingSender {
            DeliveryEventResult = Result.Success(entry),
        };
        MailRelayProviderEventsController controller = CreateProviderEventsController(
            sender,
            new ProviderWebhookAuthorizer(
                Options.Create(new MailRelayOptions {
                    RequireMailgunWebhookSignature = false,
                }),
                new HttpClient()));

        IActionResult result = await controller.IngestMailgun(new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com", Id: "provider-id", Severity: "permanent"),
            Signature: null));

        CreatedResult created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/email/providers/mailgun/events", created.Location);
        MailRelayDeliveryEventHttpResponse response = Assert.IsType<MailRelayDeliveryEventHttpResponse>(created.Value);
        Assert.Equal(entry.Id, response.Id);
        Assert.IsType<IngestMailRelayDeliveryEventCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ProviderEventsController_WhenAwsSnsSignatureIsInvalid_ReturnsUnauthorized() {
        MailRelayProviderEventsController controller = CreateProviderEventsController(
            new RecordingSender(),
            new ProviderWebhookAuthorizer(
                Options.Create(new MailRelayOptions {
                    RequireAwsSesSnsSignature = true,
                }),
                new HttpClient(new RecordingHttpMessageHandler())));

        IActionResult result = await controller.IngestAwsSesSns(new AwsSesSnsWebhookHttpRequest(
            Type: "Notification",
            Message: "{}"));

        UnauthorizedObjectResult unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(unauthorized.Value);
        Assert.Equal("MailRelay.ProviderWebhook.Unauthorized", response.Error);
    }

    [Fact]
    public async Task ProviderEventsController_WhenAwsSnsNotificationIsAccepted_ReturnsCreatedResponse() {
        MailRelayDeliveryEventEntry entry = CreateDeliveryEvent();
        var sender = new RecordingSender {
            DeliveryEventsResult = Result.Success<IReadOnlyList<MailRelayDeliveryEventEntry>>([entry]),
        };
        MailRelayProviderEventsController controller = CreateProviderEventsController(
            sender,
            new ProviderWebhookAuthorizer(
                Options.Create(new MailRelayOptions {
                    RequireAwsSesSnsSignature = false,
                }),
                new HttpClient()));

        const string message = """
                               {
                                 "notificationType": "Complaint",
                                 "mail": { "messageId": "provider-id" },
                                 "complaint": {
                                   "complainedRecipients": [
                                     { "emailAddress": "user@example.com" }
                                   ]
                                 }
                               }
                               """;

        IActionResult result = await controller.IngestAwsSesSns(new AwsSesSnsWebhookHttpRequest(
            Type: "Notification",
            Message: message));

        CreatedResult created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/email/providers/aws-ses/sns", created.Location);
        MailRelayProviderIngestionHttpResponse response = Assert.IsType<MailRelayProviderIngestionHttpResponse>(created.Value);
        Assert.Equal(1, response.Accepted);
        Assert.IsType<IngestManyMailRelayDeliveryEventsCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ProviderEventsController_WhenAwsSnsPayloadIsInvalid_ReturnsBadRequest() {
        MailRelayProviderEventsController controller = CreateProviderEventsController(
            new RecordingSender(),
            new ProviderWebhookAuthorizer(
                Options.Create(new MailRelayOptions {
                    RequireAwsSesSnsSignature = false,
                }),
                new HttpClient()));

        IActionResult result = await controller.IngestAwsSesSns(new AwsSesSnsWebhookHttpRequest(
            Type: "SubscriptionConfirmation",
            Message: "{}"));

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(badRequest.Value);
        Assert.Equal("MailRelay.ProviderWebhook.InvalidPayload", response.Error);
    }

    [Fact]
    public async Task HealthController_ReturnsHealthAndReadinessResponses() {
        var sender = new RecordingSender {
            ReadinessResult = Result.Success(),
        };
        var controller = new MailRelayHealthController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace",
                },
            },
        };

        OkObjectResult health = Assert.IsType<OkObjectResult>(controller.GetHealth());
        MailRelayHealthHttpResponse healthResponse = Assert.IsType<MailRelayHealthHttpResponse>(health.Value);
        Assert.Equal("ok", healthResponse.Status);

        OkObjectResult ready = Assert.IsType<OkObjectResult>(await controller.GetReady());
        MailRelayHealthHttpResponse readyResponse = Assert.IsType<MailRelayHealthHttpResponse>(ready.Value);
        Assert.Equal("ready", readyResponse.Status);
        Assert.Equal("ready", MailRelayHealthHttpMappings.ToReadyHttpResponse().Status);
        Assert.NotNull(MailRelayHealthHttpMappings.ToReadinessQuery());
    }

    [Fact]
    public void AddMailRelayPresentation_RegistersFiltersAuthorizerAndInvalidModelStateFactory() {
        var services = new ServiceCollection();

        services.AddMailRelayPresentation();
        using ServiceProvider provider = services.BuildServiceProvider();
        ApiBehaviorOptions options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        var actionContext = new ActionContext(
            new DefaultHttpContext {
                TraceIdentifier = "trace",
            },
            new RouteData(),
            new ActionDescriptor());
        actionContext.ModelState.AddModelError("Request.Email", "");

        BadRequestObjectResult result = Assert.IsType<BadRequestObjectResult>(options.InvalidModelStateResponseFactory(actionContext));
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(result.Value);

        Assert.NotNull(provider.GetRequiredService<RelayApiKeyAuthorizationFilter>());
        Assert.NotNull(provider.GetRequiredService<MailRelayTelemetryActionFilter>());
        Assert.NotNull(provider.GetRequiredService<ProviderWebhookAuthorizer>());
        Assert.Equal("Validation.Invalid", response.Error);
        Assert.Contains("request.email", response.Errors!.Keys);
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

    [Fact]
    public async Task TelemetryActionFilter_WhenActionHasException_RecordsFailureOutcome() {
        var filter = new MailRelayTelemetryActionFilter(NullLogger<MailRelayTelemetryActionFilter>.Instance);
        ActionExecutingContext context = CreateActionExecutingContext(new TestController());
        var exception = new InvalidOperationException("failed");

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context,
            context.Filters,
            context.Controller) {
            Exception = exception,
        }));
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

    private static AwsSesSnsWebhookHttpRequest CreateSignedAwsSnsRequest(
        string signature,
        string signingCertUrl,
        string type = "Notification") =>
        new(
            Type: type,
            Message: "{}",
            MessageId: "message-id",
            TopicArn: "arn:aws:sns:us-east-1:123456789012:topic",
            Subject: "subject",
            Timestamp: DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            SignatureVersion: "2",
            Signature: signature,
            SigningCertURL: signingCertUrl,
            SubscribeURL: "https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription",
            Token: "token");

    private static MailRelayProviderEventsController CreateProviderEventsController(
        ISender sender,
        ProviderWebhookAuthorizer authorizer) =>
        new(sender, authorizer) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace",
                },
            },
        };

    private static MailRelayDeliveryEventEntry CreateDeliveryEvent() =>
        new(
            Guid.NewGuid(),
            "bounce",
            "user@example.com",
            "test",
            "hard",
            "provider-id",
            "reason",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null) : HttpMessageHandler {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            WasCalled = true;
            return Task.FromResult(responseFactory?.Invoke(request) ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingSender : ISender {
        public object? LastRequest { get; private set; }
        public Result<MailRelayDeliveryEventEntry> DeliveryEventResult { get; init; } = Result.Success(CreateDeliveryEvent());
        public Result<IReadOnlyList<MailRelayDeliveryEventEntry>> DeliveryEventsResult { get; init; } =
            Result.Success<IReadOnlyList<MailRelayDeliveryEventEntry>>([CreateDeliveryEvent()]);
        public Result ReadinessResult { get; init; } = Result.Success();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            LastRequest = request;
            object result = request switch {
                IngestMailRelayDeliveryEventCommand => DeliveryEventResult,
                IngestManyMailRelayDeliveryEventsCommand => DeliveryEventsResult,
                CheckMailRelayReadinessQuery => ReadinessResult,
                _ => throw new InvalidOperationException($"Unexpected request type {request.GetType().FullName}."),
            };
            return Task.FromResult((TResponse)result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
            LastRequest = request;
            return Task.FromResult<object?>(null);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<object?>();
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
