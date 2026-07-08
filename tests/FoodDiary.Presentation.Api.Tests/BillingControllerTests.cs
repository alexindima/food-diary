using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Commands.CreateCheckoutSession;
using FoodDiary.Application.Billing.Commands.CreatePortalSession;
using FoodDiary.Application.Billing.Commands.StartPremiumTrial;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Billing.Queries.GetBillingOverview;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Billing;
using FoodDiary.Presentation.Api.Features.Billing.Requests;
using FoodDiary.Presentation.Api.Features.Billing.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingControllerTests {
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

    private static BillingController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

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
