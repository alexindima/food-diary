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

        return new BillingWebhookController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext,
            },
        };
    }

}
