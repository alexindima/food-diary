using FoodDiary.Presentation.Api.Tests;
using System.Reflection;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.CreatePortalSession;
using FoodDiary.Modules.Billing.Application.Commands.StartPremiumTrial;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Presentation.Controllers;
using FoodDiary.Modules.Billing.Presentation.Requests;
using FoodDiary.Modules.Billing.Presentation.Responses;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Billing.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingControllerTests {
    private const string RequestId = "checkout-request-id";

    [Fact]
    public async Task GetOverview_SendsQueryAndReturnsResponse() {
        BillingOverviewModel model = CreateOverview();
        IRequest<Result<BillingOverviewModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        BillingController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetOverview(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        BillingOverviewHttpResponse response = Assert.IsType<BillingOverviewHttpResponse>(ok.Value);
        Assert.True(response.IsPremium);
        GetBillingOverviewQuery query = Assert.IsType<GetBillingOverviewQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task StartPremiumTrial_SendsCommandAndReturnsOverview() {
        BillingOverviewModel model = CreateOverview();
        IRequest<Result<BillingOverviewModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        BillingController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.StartPremiumTrial(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        BillingOverviewHttpResponse response = Assert.IsType<BillingOverviewHttpResponse>(ok.Value);
        Assert.True(response.IsPremium);
        StartPremiumTrialCommand command = Assert.IsType<StartPremiumTrialCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public async Task CreateCheckoutSession_SendsCommandAndReturnsSession() {
        var model = new BillingCheckoutSessionModel("session-1", "https://checkout.example", "customer-1", "price-1", "premium");
        IRequest<Result<BillingCheckoutSessionModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        BillingController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new CreateCheckoutSessionHttpRequest("premium", "stripe");

        IActionResult result = await controller.CreateCheckoutSession(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        CheckoutSessionHttpResponse response = Assert.IsType<CheckoutSessionHttpResponse>(ok.Value);
        Assert.Equal("session-1", response.SessionId);
        CreateCheckoutSessionCommand command = Assert.IsType<CreateCheckoutSessionCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("premium", command.Plan);
        Assert.Equal("stripe", command.Provider);
        Assert.Equal(RequestId, command.IdempotencyKey);
    }

    [Fact]
    public async Task CreatePortalSession_SendsCommandAndReturnsSession() {
        var model = new BillingPortalSessionModel("https://portal.example");
        IRequest<Result<BillingPortalSessionModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        BillingController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.CreatePortalSession(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PortalSessionHttpResponse response = Assert.IsType<PortalSessionHttpResponse>(ok.Value);
        Assert.Equal("https://portal.example", response.Url);
        CreatePortalSessionCommand command = Assert.IsType<CreatePortalSessionCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
    }

    private static BillingController CreateController(ISender sender) {
        var httpContext = new DefaultHttpContext();
        MethodInfo setRequestId = typeof(IdempotencyRequestContext).GetMethod(
            "SetRequestId",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Idempotency request context setter was not found.");
        setRequestId.Invoke(null, [httpContext, RequestId]);
        return new BillingController(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = httpContext,
            },
        };
    }

    private static BillingOverviewModel CreateOverview() =>
        new(
            IsPremium: true,
            SubscriptionStatus: "active",
            Plan: "premium",
            SubscriptionProvider: "stripe",
            CurrentPeriodStartUtc: null,
            CurrentPeriodEndUtc: null,
            NextBillingAttemptUtc: null,
            CancelAtPeriodEnd: false,
            RenewalEnabled: true,
            ManageBillingAvailable: true,
            PremiumTrialStartUtc: null,
            PremiumTrialEndUtc: null,
            PremiumTrialActive: false,
            PremiumTrialUsed: true,
            CanStartPremiumTrial: false,
            Provider: "stripe",
            PaddleClientToken: null,
            AvailableProviders: ["stripe"]);
}
