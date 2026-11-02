using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingCancellations;
using FoodDiary.Modules.Admin.Presentation.Controllers;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminBillingCancellationControllerTests {
    [Fact]
    public async Task Report_ForwardsFiltersAndMapsHistoryPagingAndFeedback() {
        DateTime requested = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var item = new AdminBillingCancellationReadModel(Guid.NewGuid(), Guid.NewGuid(), "subscriber@example.test",
            "paddle", "sub_123", "monthly", "user", "scheduled", requested, requested.AddDays(10),
            "too_expensive", "Price", requested.AddMinutes(1));
        var report = new AdminBillingCancellationReportReadModel([item], 2, 10, 21, 3, 7, [new("too_expensive", 4)]);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(report));
        var controller = new AdminBillingController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        var filters = new GetAdminBillingHttpQuery(Page: 2, Limit: 10, Provider: "paddle", Status: "scheduled",
            Search: "subscriber", FromUtc: requested.AddDays(-1), ToUtc: requested);

        AdminBillingCancellationReportHttpResponse response = Assert.IsType<AdminBillingCancellationReportHttpResponse>(Assert.IsType<OkObjectResult>(await controller.GetCancellations(filters)).Value);

        GetAdminBillingCancellationsQuery query = Assert.IsType<GetAdminBillingCancellationsQuery>(sender.Request);
        AdminBillingCancellationHttpResponse history = Assert.Single(response.Data);
        AdminBillingCancellationReasonHttpResponse reason = Assert.Single(response.Reasons);
        Assert.Multiple(
            () => Assert.Equal(new GetAdminBillingCancellationsQuery(2, 10, "paddle", "scheduled", "subscriber", filters.FromUtc, filters.ToUtc), query),
            () => Assert.Equal(item.Id, history.Id),
            () => Assert.Equal(item.UserId, history.UserId),
            () => Assert.Equal(item.UserEmail, history.UserEmail),
            () => Assert.Equal(item.Provider, history.Provider),
            () => Assert.Equal(item.ExternalSubscriptionId, history.ExternalSubscriptionId),
            () => Assert.Equal(item.Plan, history.Plan),
            () => Assert.Equal(item.Source, history.Source),
            () => Assert.Equal(item.Outcome, history.Outcome),
            () => Assert.Equal(item.RequestedAtUtc, history.RequestedAtUtc),
            () => Assert.Equal(item.EffectiveAtUtc, history.EffectiveAtUtc),
            () => Assert.Equal(item.ReasonCode, history.ReasonCode),
            () => Assert.Equal(item.Comment, history.Comment),
            () => Assert.Equal(item.FeedbackSubmittedAtUtc, history.FeedbackSubmittedAtUtc),
            () => Assert.Equal(2, response.Page),
            () => Assert.Equal(10, response.Limit),
            () => Assert.Equal(21, response.TotalItems),
            () => Assert.Equal(3, response.TotalPages),
            () => Assert.Equal(7, response.FeedbackCount),
            () => Assert.Equal("too_expensive", reason.ReasonCode),
            () => Assert.Equal(4, reason.Count));
    }
}
