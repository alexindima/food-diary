using System.Text;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Webhooks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingWebhookControllerTests {
    [Theory]
    [InlineData("Paddle", "Paddle-Signature", "paddle-signature")]
    [InlineData("Stripe", "Stripe-Signature", "stripe-signature")]
    public async Task HandleWebhook_WithSignedProvider_SendsPayloadAndSignature(string provider, string headerName, string signature) {
        var sender = new RecordingSender(Result.Success());
        BillingWebhookController controller = CreateController(sender, payload: "{\"event\":\"paid\"}");
        controller.Request.Headers[headerName] = signature;

        IActionResult result = await controller.HandleWebhook(provider);

        Assert.IsType<NoContentResult>(result);
        ProcessBillingWebhookCommand command = Assert.IsType<ProcessBillingWebhookCommand>(sender.Request);
        Assert.Equal(provider, command.Provider);
        Assert.Equal("{\"event\":\"paid\"}", command.Payload);
        Assert.Equal(signature, command.SignatureHeader);
        Assert.Equal(0, controller.Request.Body.Position);
    }

    [Fact]
    public async Task HandleWebhook_WithYooKassaProvider_SendsEmptySignatureHeader() {
        var sender = new RecordingSender(Result.Success());
        BillingWebhookController controller = CreateController(sender, payload: "{\"event\":\"payment.succeeded\"}");
        controller.Request.Headers["Stripe-Signature"] = "ignored-stripe-signature";
        controller.Request.Headers["Paddle-Signature"] = "ignored-paddle-signature";

        IActionResult result = await controller.HandleWebhook("YooKassa");

        Assert.IsType<NoContentResult>(result);
        ProcessBillingWebhookCommand command = Assert.IsType<ProcessBillingWebhookCommand>(sender.Request);
        Assert.Equal("YooKassa", command.Provider);
        Assert.Equal("{\"event\":\"payment.succeeded\"}", command.Payload);
        Assert.Equal(string.Empty, command.SignatureHeader);
    }

    [Fact]
    public async Task HandleWebhook_WhenCommandFails_ReturnsApiErrorResponse() {
        var sender = new RecordingSender(Result.Failure(Errors.Validation.Invalid("Provider", "Unsupported provider.")));
        BillingWebhookController controller = CreateController(sender, payload: "{}");
        controller.HttpContext.TraceIdentifier = "trace-webhook";

        IActionResult result = await controller.HandleWebhook("UnknownProvider");

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("trace-webhook", response.TraceId);
    }

    private static BillingWebhookController CreateController(RecordingSender sender, string payload) {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        httpContext.Request.ContentLength = httpContext.Request.Body.Length;

        return new BillingWebhookController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext,
            },
        };
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingSender(Result response) : ISender {
        public object? Request { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            Request = request;
            CancellationToken = cancellationToken;
            return Task.FromResult((TResponse)(object)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest {
            Request = request;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
            Request = request;
            CancellationToken = cancellationToken;
            return Task.FromResult<object?>(response);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }
    }
}
