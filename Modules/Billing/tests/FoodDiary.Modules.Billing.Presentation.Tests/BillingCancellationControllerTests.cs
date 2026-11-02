using FoodDiary.Modules.Billing.Application.Commands.CancelSubscription;
using FoodDiary.Modules.Billing.Application.Commands.SubmitCancellationFeedback;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Presentation.Controllers;
using FoodDiary.Modules.Billing.Presentation.Requests;
using FoodDiary.Modules.Billing.Presentation.Responses;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Billing.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingCancellationControllerTests {
    [Fact]
    public async Task Cancel_ReturnsTheConfirmedCancellationAndPaidAccessState() {
        DateTime now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var overview = new BillingOverviewModel(IsPremium: true, SubscriptionStatus: "active", Plan: "monthly", SubscriptionProvider: "Paddle",
            CurrentPeriodStartUtc: now, CurrentPeriodEndUtc: now.AddDays(30), NextBillingAttemptUtc: null,
            CancelAtPeriodEnd: true, RenewalEnabled: false, ManageBillingAvailable: true,
            PremiumTrialStartUtc: null, PremiumTrialEndUtc: null, PremiumTrialActive: false, PremiumTrialUsed: true,
            CanStartPremiumTrial: false, Provider: "Paddle", PaddleClientToken: null, AvailableProviders: ["Paddle"]);
        var model = new BillingCancellationModel(Guid.NewGuid(), overview);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(model));
        var controller = new BillingController(sender) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var id = Guid.NewGuid();

        CancelSubscriptionHttpResponse response = Assert.IsType<CancelSubscriptionHttpResponse>(Assert.IsType<OkObjectResult>(await controller.CancelSubscription(id)).Value);

        Assert.Multiple(
            () => Assert.Equal(id, Assert.IsType<CancelSubscriptionCommand>(sender.Request).UserId),
            () => Assert.Equal(model.CancellationId, response.CancellationId),
            () => Assert.True(response.Overview.IsPremium),
            () => Assert.True(response.Overview.CancelAtPeriodEnd),
            () => Assert.False(response.Overview.RenewalEnabled),
            () => Assert.Equal(overview.CurrentPeriodEndUtc, response.Overview.CurrentPeriodEndUtc));
    }

    [Fact]
    public async Task Feedback_UsesTheAuthenticatedUserAndTheCancellationFromTheRoute() {
        CapturedSender sender = SubstituteSender.Capture(Result.Success());
        var controller = new BillingController(sender) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var id = Guid.NewGuid();
        var cancellationId = Guid.NewGuid();
        var feedback = new SubmitCancellationFeedbackHttpRequest { ReasonCode = "other", Comment = "Useful feedback" };

        Assert.IsType<NoContentResult>(await controller.SubmitCancellationFeedback(id, cancellationId, feedback));

        SubmitCancellationFeedbackCommand command = Assert.IsType<SubmitCancellationFeedbackCommand>(sender.Request);
        Assert.Multiple(
            () => Assert.Equal(id, command.UserId),
            () => Assert.Equal(new BillingCancellationId(cancellationId), command.CancellationId),
            () => Assert.Equal(feedback.ReasonCode, command.ReasonCode),
            () => Assert.Equal(feedback.Comment, command.Comment));
    }
}
