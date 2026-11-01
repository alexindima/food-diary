using System.Net;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public sealed partial class BillingGatewayTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PaddleCreateCheckoutSession_WhenFinalPagesStillContainNext_CreatesCheckout(bool existingCustomer) {
        var responses = new List<HttpResponseMessage>();
        if (!existingCustomer) {
            responses.Add(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent("""
                    {
                      "data": [],
                      "meta": { "pagination": { "has_more": false, "next": "https://api.paddle.test/customers?after=last" } }
                    }
                    """),
            });
            responses.Add(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent("""{ "data": { "id": "ctm_new" } }"""),
            });
        }
        responses.Add(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "data": [],
                  "meta": { "pagination": { "has_more": false, "next": "https://api.paddle.test/transactions?after=last" } }
                }
                """),
        });
        responses.Add(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""{ "data": { "id": "txn_new", "checkout": { "url": "https://checkout.paddle.com/txn_new" } } }"""),
        });
        var handler = new RecordingHttpMessageHandler([.. responses]);
        var gateway = new PaddleBillingGateway(new HttpClient(handler), MsOptions.Create(new PaddleOptions {
            ApiKey = "paddle-api-key",
            ApiBaseUrl = "https://api.paddle.test",
            PremiumMonthlyPriceId = "pri_monthly",
            PremiumYearlyPriceId = "pri_yearly",
            CheckoutUrl = "https://app.example/billing/paddle",
        }));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", existingCustomer ? "ctm_known" : null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, "Checkout must continue after a terminal Paddle page. " + result.Error.Message);
        Assert.Equal("txn_new", result.Value.SessionId);
        Assert.Equal(existingCustomer ? 2 : 4, handler.Requests.Count);
        Assert.Equal(existingCustomer ? 1 : 2, handler.Requests.Count(request => request.Method == HttpMethod.Post));
        Assert.EndsWith("/transactions", handler.Requests[^1].RequestUri?.AbsolutePath, StringComparison.Ordinal);
    }
}
