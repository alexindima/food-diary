using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FoodDiary.MailRelay.Client.Models;
using FoodDiary.Results;
using FoodDiary.MailRelay.Application.Emails.Commands;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Queries;
using FoodDiary.MailRelay.Application.Health;
using FoodDiary.MailRelay.Application.Options;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Presentation.Controllers;
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

namespace FoodDiary.MailRelay.Presentation.Tests;

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
            new Error("code", "message", kind),
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
        var error = new Error("code", "message", ErrorKind.Conflict);

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
        var filter = new RelayApiKeyAuthorizationFilter(Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
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
        var filter = new RelayApiKeyAuthorizationFilter(Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
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
        var filter = new RelayApiKeyAuthorizationFilter(Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
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
        string timestamp = FixedNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
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
        string timestamp = FixedNow.AddHours(-1).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
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
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                RequireMailgunWebhookSignature = false,
            }),
            new HttpClient(new RecordingHttpMessageHandler()),
            FixedTime);

        Assert.True(authorizer.IsMailgunAuthorized(new MailgunWebhookHttpRequest(
            new MailgunEventDataHttpRequest("failed", "user@example.com"),
            Signature: null)));
    }

    [Fact]
    public void ProviderWebhookAuthorizer_WhenMailgunKeyOrSignatureIsMissing_RejectsRequest() {
        var missingKey = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                RequireMailgunWebhookSignature = true,
            }),
            new HttpClient(new RecordingHttpMessageHandler()),
            FixedTime);
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
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(handler),
            FixedTime);
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
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                RequireAwsSesSnsSignature = false,
            }),
            new HttpClient(new RecordingHttpMessageHandler()),
            FixedTime);

        Assert.True(await authorizer.IsAwsSesSnsAuthorizedAsync(new AwsSesSnsWebhookHttpRequest(
            Type: "",
            Message: null), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsRequiredFieldsAreMissing_RejectsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler()),
            FixedTime);

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(new AwsSesSnsWebhookHttpRequest(
            Type: "Notification",
            Message: "{}"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsSignatureIsNotBase64_RejectsRequest() {
        var handler = new RecordingHttpMessageHandler();
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(handler),
            FixedTime);

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: "not-base64",
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsCertificateDownloadFails_RejectsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => throw new HttpRequestException("download failed"))),
            FixedTime);

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsCertificatePemIsInvalid_RejectsRequest() {
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent("not a pem"),
            })),
            FixedTime);

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsSignatureIsValid_AllowsRequest() {
        using var rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateSelfSignedCertificate(rsa);
        AwsSesSnsWebhookHttpRequest request = CreateSignedAwsSnsRequest(
            signature: "placeholder",
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem");
        string canonical = InvokeSnsCanonicalString(request);
        byte[] signature = rsa.SignData(
            Encoding.UTF8.GetBytes(canonical),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request = request with {
            Signature = Convert.ToBase64String(signature),
        };
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent(certificate.ExportCertificatePem()),
            })),
            FixedTime,
            _ => true);

        Assert.True(await authorizer.IsAwsSesSnsAuthorizedAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsCertificateHasNoRsaKey_RejectsRequest() {
        using var ecdsa = ECDsa.Create();
        CertificateRequest certificateRequest = new("CN=sns.amazonaws.com", ecdsa, HashAlgorithmName.SHA256);
        using X509Certificate2 certificate = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(10));
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent(certificate.ExportCertificatePem()),
            })),
            FixedTime,
            _ => true);

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsCertificateChainIsInvalid_RejectsRequest() {
        using var rsa = RSA.Create(2048);
        using X509Certificate2 certificate = CreateSelfSignedCertificate(rsa);
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent(certificate.ExportCertificatePem()),
            })),
            FixedTime);

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem"), CancellationToken.None));
    }

    [Fact]
    public async Task ProviderWebhookAuthorizer_WhenAwsSnsConfirmationMessageHasInvalidPem_BuildsConfirmationCanonicalString() {
        var authorizer = new ProviderWebhookAuthorizer(
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions()),
            new HttpClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StringContent("not a pem"),
            })),
            FixedTime);

        Assert.False(await authorizer.IsAwsSesSnsAuthorizedAsync(CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.amazonaws.com/SimpleNotificationService-test.pem",
            type: "SubscriptionConfirmation"), CancellationToken.None));
    }

    [Theory]
    [InlineData("sns.amazonaws.com", true)]
    [InlineData("sns.us-east-1.amazonaws.com", true)]
    [InlineData("sns.eu-central-1.amazonaws.com", true)]
    [InlineData("sns.cn-north-1.amazonaws.com.cn", true)]
    [InlineData("email.amazonaws.com", false)]
    [InlineData("sns.extra.amazonaws.com", false)]
    [InlineData("sns.us-east.amazonaws.com", false)]
    [InlineData("example.com", false)]
    public void ProviderWebhookAuthorizer_IsTrustedSnsCertificateHost_ReturnsExpectedResult(
        string host,
        bool expected) {
        MethodInfo method = typeof(ProviderWebhookAuthorizer).GetMethod(
            "IsTrustedSnsCertificateHost",
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(string)])!;

        bool actual = (bool)method.Invoke(null, [host])!;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProviderWebhookAuthorizer_CreateSnsCanonicalString_IncludesSubjectAndConfirmationFields() {
        AwsSesSnsWebhookHttpRequest request = CreateSignedAwsSnsRequest(
            signature: Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid")),
            signingCertUrl: "https://sns.amazonaws.com/SimpleNotificationService-test.pem",
            type: "SubscriptionConfirmation");
        string canonical = InvokeSnsCanonicalString(request);

        Assert.Contains("Subject\nsubject\n", canonical, StringComparison.Ordinal);
        Assert.Contains("SubscribeURL\nhttps://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription\n", canonical, StringComparison.Ordinal);
        Assert.Contains("Token\ntoken\n", canonical, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderWebhookAuthorizer_TryValidateMailgunTimestamp_WhenTimestampOverflows_ReturnsFalse() {
        MethodInfo method = typeof(ProviderWebhookAuthorizer).GetMethod(
            "TryValidateMailgunTimestamp",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        bool actual = (bool)method.Invoke(null, [long.MaxValue.ToString(CultureInfo.InvariantCulture), FixedNow])!;

        Assert.False(actual);
    }

    [Fact]
    public void ProviderWebhookAuthorizer_FixedTimeEquals_NormalizesActualSignature() {
        MethodInfo method = typeof(ProviderWebhookAuthorizer).GetMethod(
            "FixedTimeEquals",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        bool actual = (bool)method.Invoke(null, ["abcdef", " ABCDEF "])!;

        Assert.True(actual);
    }

    [Fact]
    public async Task ProviderEventsController_WhenMailgunSignatureIsInvalid_ReturnsUnauthorized() {
        MailRelayProviderEventsController controller = CreateProviderEventsController(
            new RecordingSender(),
            new ProviderWebhookAuthorizer(
                Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                    RequireMailgunWebhookSignature = true,
                    MailgunWebhookSigningKey = "mailgun-secret",
                }),
                new HttpClient(new RecordingHttpMessageHandler()),
                FixedTime));

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
                Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                    RequireMailgunWebhookSignature = false,
                }),
                new HttpClient(new RecordingHttpMessageHandler()),
                FixedTime));

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
                Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                    RequireAwsSesSnsSignature = true,
                }),
                new HttpClient(new RecordingHttpMessageHandler()),
                FixedTime));

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
                Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                    RequireAwsSesSnsSignature = false,
                }),
                new HttpClient(new RecordingHttpMessageHandler()),
                FixedTime));

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
                Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                    RequireAwsSesSnsSignature = false,
                }),
                new HttpClient(new RecordingHttpMessageHandler()),
                FixedTime));

        IActionResult result = await controller.IngestAwsSesSns(new AwsSesSnsWebhookHttpRequest(
            Type: "SubscriptionConfirmation",
            Message: "{}"));

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        MailRelayApiErrorHttpResponse response = Assert.IsType<MailRelayApiErrorHttpResponse>(badRequest.Value);
        Assert.Equal("MailRelay.ProviderWebhook.InvalidPayload", response.Error);
    }

    [Fact]
    public async Task QueueController_ReturnsStatsAndAcceptedEnqueueResponse() {
        var sender = new RecordingSender {
            QueueStatsResult = Result.Success(new MailRelayQueueStats(1, 2, 3, 4, 5, 6)),
            EnqueueResult = Result.Success(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        };
        var controller = new MailRelayQueueController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace",
                },
            },
        };

        OkObjectResult statsResult = Assert.IsType<OkObjectResult>(await controller.GetStats());
        MailRelayQueueStatsHttpResponse stats = Assert.IsType<MailRelayQueueStatsHttpResponse>(statsResult.Value);
        Assert.Equal(1, stats.PendingCount);
        Assert.IsType<GetMailRelayQueueStatsQuery>(sender.LastRequest);

        AcceptedResult enqueueResult = Assert.IsType<AcceptedResult>(await controller.Enqueue(new EnqueueMailRelayEmailRequest(
            "relay@example.com",
            "Relay",
            ["user@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello")));
        EnqueueMailRelayEmailResponse enqueueResponse = Assert.IsType<EnqueueMailRelayEmailResponse>(enqueueResult.Value);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), enqueueResponse.Id);
        Assert.Equal("/api/email/messages/11111111-1111-1111-1111-111111111111", enqueueResult.Location);
        Assert.IsType<EnqueueMailRelayEmailCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task SuppressionsController_ReturnsListCreatedAndNoContentResponses() {
        var suppression = new MailRelaySuppressionEntry(
            "user@example.com",
            "manual",
            "test",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            ExpiresAtUtc: null);
        var sender = new RecordingSender {
            SuppressionsResult = Result.Success<IReadOnlyList<MailRelaySuppressionEntry>>([suppression]),
            SuppressionCreateResult = Result.Success(),
            SuppressionRemoveResult = Result.Success(),
        };
        var controller = new MailRelaySuppressionsController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace",
                },
            },
        };

        OkObjectResult getResult = Assert.IsType<OkObjectResult>(await controller.Get("user@example.com"));
        IReadOnlyList<MailRelaySuppressionHttpResponse> suppressions = Assert.IsAssignableFrom<IReadOnlyList<MailRelaySuppressionHttpResponse>>(getResult.Value);
        Assert.Single(suppressions);
        Assert.IsType<GetMailRelaySuppressionsQuery>(sender.LastRequest);

        CreatedResult createResult = Assert.IsType<CreatedResult>(await controller.Create(new CreateMailRelaySuppressionHttpRequest(
            "user@example.com",
            "manual",
            "test")));
        Assert.Equal("/api/email/suppressions?email=user%40example.com", createResult.Location);
        Assert.IsType<CreateMailRelaySuppressionCommand>(sender.LastRequest);

        NoContentResult deleteResult = Assert.IsType<NoContentResult>(await controller.Delete("user@example.com"));
        Assert.Equal(StatusCodes.Status204NoContent, deleteResult.StatusCode);
        Assert.IsType<RemoveMailRelaySuppressionCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ControllerBase_SendWithoutResponse_UsesHttpContextCancellationToken() {
        var sender = new RecordingSender();
        var controller = new ExposedMailRelayController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    TraceIdentifier = "trace",
                },
            },
        };

        await controller.SendCommandAsync(new SimpleCommand());

        Assert.IsType<SimpleCommand>(sender.LastRequest);
    }

    [Fact]
    public void MailRelayApiErrorHttpResponse_CanCarryMessageTraceAndErrors() {
        var response = new MailRelayApiErrorHttpResponse(
            "error",
            "message",
            "trace",
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["field"] = ["invalid"],
            });

        Assert.Equal("message", response.Message);
        Assert.Equal("trace", response.TraceId);
        Assert.Contains("field", response.Errors!.Keys);
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

    [Fact]
    public async Task TelemetryActionFilter_WhenControllerHasNoFeatureNamespace_UsesUnknownFeature() {
        var filter = new MailRelayTelemetryActionFilter(NullLogger<MailRelayTelemetryActionFilter>.Instance);
        ActionExecutingContext context = CreateActionExecutingContext(new ControllerWithoutNamespace());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context,
            context.Filters,
            context.Controller)));
    }

    [Fact]
    public void TelemetryActionFilter_ResolveFeatureName_WhenTypeHasNoNamespace_ReturnsUnknown() {
        AssemblyName assemblyName = new("MailRelayDynamicTestAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MailRelayDynamicTestModule");
        Type controllerType = moduleBuilder
            .DefineType("NoNamespaceController", TypeAttributes.Public, typeof(ControllerBase))
            .CreateType()!;
        MethodInfo method = typeof(MailRelayTelemetryActionFilter).GetMethod(
            "ResolveFeatureName",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        string feature = (string)method.Invoke(null, [controllerType])!;

        Assert.Equal("Unknown", feature);
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
            Microsoft.Extensions.Options.Options.Create(new MailRelayOptions {
                RequireMailgunWebhookSignature = true,
                MailgunWebhookSigningKey = mailgunSigningKey,
            }),
            new HttpClient(new RecordingHttpMessageHandler()),
            FixedTime);

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

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

    private static string InvokeSnsCanonicalString(AwsSesSnsWebhookHttpRequest request) {
        MethodInfo method = typeof(ProviderWebhookAuthorizer).GetMethod(
            "CreateSnsCanonicalString",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (string)method.Invoke(null, [request])!;
    }

    private static X509Certificate2 CreateSelfSignedCertificate(RSA rsa) {
        CertificateRequest certificateRequest = new(
            "CN=sns.amazonaws.com",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(10));
    }

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
        public Result<MailRelayQueueStats> QueueStatsResult { get; init; } =
            Result.Success(new MailRelayQueueStats(0, 0, 0, 0, 0, 0));
        public Result<Guid> EnqueueResult { get; init; } = Result.Success(Guid.NewGuid());
        public Result<IReadOnlyList<MailRelaySuppressionEntry>> SuppressionsResult { get; init; } =
            Result.Success<IReadOnlyList<MailRelaySuppressionEntry>>([]);
        public Result SuppressionCreateResult { get; init; } = Result.Success();
        public Result SuppressionRemoveResult { get; init; } = Result.Success();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            LastRequest = request;
            object result = request switch {
                IngestMailRelayDeliveryEventCommand => DeliveryEventResult,
                IngestManyMailRelayDeliveryEventsCommand => DeliveryEventsResult,
                CheckMailRelayReadinessQuery => ReadinessResult,
                GetMailRelayQueueStatsQuery => QueueStatsResult,
                EnqueueMailRelayEmailCommand => EnqueueResult,
                GetMailRelaySuppressionsQuery => SuppressionsResult,
                CreateMailRelaySuppressionCommand => SuppressionCreateResult,
                RemoveMailRelaySuppressionCommand => SuppressionRemoveResult,
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

    [ExcludeFromCodeCoverage]
    private sealed class ControllerWithoutNamespace : ControllerBase {
    }

    [ExcludeFromCodeCoverage]
    private sealed class ExposedMailRelayController(ISender sender) : MailRelayControllerBase(sender) {
        public Task SendCommandAsync(IRequest request) => Send(request);
    }

    [ExcludeFromCodeCoverage]
    private sealed record SimpleCommand : IRequest;
}
