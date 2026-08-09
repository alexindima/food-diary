using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Text;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Features.Billing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingWebhookControllerTests {
    [Fact]
    public void HandleWebhook_HasPaymentSpecificRequestBodyLimit() {
        MethodInfo method = typeof(BillingWebhookController).GetMethod(nameof(BillingWebhookController.HandleWebhook))!;

        RequestSizeLimitAttribute attribute = Assert.Single(method.GetCustomAttributes<RequestSizeLimitAttribute>());

        Assert.Equal(BillingWebhookHttpProcessor.MaxWebhookPayloadBytes, ((IRequestSizeLimitMetadata)attribute).MaxRequestBodySize);
    }

    [Fact]
    public async Task HandleWebhook_WhenChunkedPayloadExceedsLimit_StopsBeforeDispatch() {
        bool dispatched = false;
        ISender sender = SubstituteSender.Create(Result.Success(), _ => dispatched = true);
        BillingWebhookController controller = CreateController(
            sender,
            payload: new string('x', BillingWebhookHttpProcessor.MaxWebhookPayloadBytes + 1));
        controller.Request.ContentLength = null;

        BadHttpRequestException exception = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            controller.HandleWebhook("Stripe"));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, exception.StatusCode);
        Assert.False(dispatched);
        Assert.Equal(0, controller.Request.Body.Position);
    }

    [Fact]
    public async Task HandleWebhook_WithNonSeekableBody_DoesNotAttemptToResetStream() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new NonSeekableReadStream(Encoding.UTF8.GetBytes("{}"));
        httpContext.Request.ContentLength = 2;
        var controller = new BillingWebhookController(sender, new BillingWebhookHttpProcessor()) {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        IActionResult result = await controller.HandleWebhook("YooKassa");

        Assert.IsType<NoContentResult>(result);
        ProcessBillingWebhookCommand command = Assert.IsType<ProcessBillingWebhookCommand>(sentRequest);
        Assert.Equal("{}", command.Payload);
    }

    [Fact]
    public async Task HandleWebhook_WhenSeekableBodyReadFails_RestoresBodyPositionAndDoesNotDispatch() {
        bool dispatched = false;
        ISender sender = SubstituteSender.Create(Result.Success(), _ => dispatched = true);
        var httpContext = new DefaultHttpContext();
        var body = new ThrowingSeekableReadStream();
        httpContext.Request.Body = body;
        httpContext.Request.ContentLength = 2;
        var controller = new BillingWebhookController(sender, new BillingWebhookHttpProcessor()) {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        await Assert.ThrowsAsync<IOException>(() => controller.HandleWebhook("Stripe"));

        Assert.False(dispatched);
        Assert.Equal(0, body.Position);
    }

    [Theory]
    [InlineData("Paddle", "Paddle-Signature", "paddle-signature")]
    [InlineData("Stripe", "Stripe-Signature", "stripe-signature")]
    public async Task HandleWebhook_WithSignedProvider_SendsPayloadAndSignature(string provider, string headerName, string signature) {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        BillingWebhookController controller = CreateController(sender, payload: "{\"event\":\"paid\"}");
        controller.Request.Headers[headerName] = signature;

        IActionResult result = await controller.HandleWebhook(provider);

        Assert.IsType<NoContentResult>(result);
        ProcessBillingWebhookCommand command = Assert.IsType<ProcessBillingWebhookCommand>(sentRequest);
        Assert.Equal(provider, command.Provider);
        Assert.Equal("{\"event\":\"paid\"}", command.Payload);
        Assert.Equal(signature, command.SignatureHeader);
        Assert.Equal(0, controller.Request.Body.Position);
    }

    [Fact]
    public async Task HandleWebhook_WithYooKassaProvider_SendsEmptySignatureHeader() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        BillingWebhookController controller = CreateController(sender, payload: "{\"event\":\"payment.succeeded\"}");
        controller.Request.Headers["Stripe-Signature"] = "ignored-stripe-signature";
        controller.Request.Headers["Paddle-Signature"] = "ignored-paddle-signature";

        IActionResult result = await controller.HandleWebhook("YooKassa");

        Assert.IsType<NoContentResult>(result);
        ProcessBillingWebhookCommand command = Assert.IsType<ProcessBillingWebhookCommand>(sentRequest);
        Assert.Equal("YooKassa", command.Provider);
        Assert.Equal("{\"event\":\"payment.succeeded\"}", command.Payload);
        Assert.Equal(string.Empty, command.SignatureHeader);
    }

    [Fact]
    public async Task HandleWebhook_WhenCommandFails_ReturnsApiErrorResponse() {
        ISender sender = SubstituteSender.Create(Result.Failure(Errors.Validation.Invalid("Provider", "Unsupported provider.")));
        BillingWebhookController controller = CreateController(sender, payload: "{}");
        controller.HttpContext.TraceIdentifier = "trace-webhook";

        IActionResult result = await controller.HandleWebhook("UnknownProvider");

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("trace-webhook", response.TraceId);
    }

    private static BillingWebhookController CreateController(ISender sender, string payload) {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        httpContext.Request.ContentLength = httpContext.Request.Body.Length;

        return new BillingWebhookController(sender, new BillingWebhookHttpProcessor()) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext,
            },
        };
    }

    [ExcludeFromCodeCoverage]
    private sealed class NonSeekableReadStream(byte[] payload) : MemoryStream(payload) {
        public override bool CanSeek => false;

        public override long Position {
            get => base.Position;
            set => throw new NotSupportedException();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingSeekableReadStream : MemoryStream {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            Position = 1;
            throw new IOException("Simulated request body read failure.");
        }
    }

}
