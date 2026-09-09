using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminAudit;
using FoodDiary.Application.Admin.Queries.GetAdminBugReports;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessagePage;
using FoodDiary.Application.Admin.Queries.GetAdminOutgoingEmails;
using FoodDiary.Application.Admin.Queries.GetAdminRetention;
using FoodDiary.Application.Admin.Queries.GetAdminTemplateRevisions;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminJournalControllerTests {
    [Fact]
    public async Task Audit_ForwardsFiltersAndMapsPage() {
        var entry = new AdminAuditEntryModel(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "read", "User", "target", "metadata", DateTime.UnixEpoch);
        var page = new AdminAuditPage([entry], 31);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(page));
        AdminAuditController controller = WithContext(new AdminAuditController(sender));
        var request = new GetAdminAuditHttpQuery(2, 10, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), entry.ActorUserId, entry.SubjectClientUserId, "read", "User", "target");
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetPage(request));
        AdminAuditPageHttpResponse response = Assert.IsType<AdminAuditPageHttpResponse>(result.Value);
        GetAdminAuditQuery query = Assert.IsType<GetAdminAuditQuery>(sender.Request);
        Assert.Multiple(
            () => Assert.Equivalent(page, response, strict: true),
            () => Assert.Equivalent(request, query.Filter, strict: true));
    }

    [Fact]
    public async Task Bugs_ForwardsFiltersAndMapsConfigurationAndContentExpiry() {
        var entry = new AdminBugReportEntry(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UnixEpoch, "subject", "processed", 2, "summary", "https://example.com/pr/1", ContentExpired: true);
        var page = new AdminBugReportPage([entry], 31) { IsConfigured = false };
        CapturedSender sender = SubstituteSender.Capture(Result.Success(page));
        AdminBugReportsController controller = WithContext(new AdminBugReportsController(sender));
        var request = new GetAdminBugReportsHttpQuery(2, 10, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), "processed", "subject", entry.Id);
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetPage(request));
        AdminBugReportPageHttpResponse response = Assert.IsType<AdminBugReportPageHttpResponse>(result.Value);
        GetAdminBugReportsQuery query = Assert.IsType<GetAdminBugReportsQuery>(sender.Request);
        Assert.Multiple(
            () => Assert.Equivalent(page, response, strict: true),
            () => Assert.Equivalent(request, query.Filter, strict: true));
    }

    [Fact]
    public async Task Inbox_ForwardsFiltersAndMapsCounters() {
        var entry = new AdminMailInboxMessageSummaryModel(Guid.NewGuid(), "from@example.com", ["to@example.com"], "subject", "general", "received", ReadAtUtc: null, DateTimeOffset.UnixEpoch);
        var page = new AdminMailInboxMessagePageModel([entry], 31, 11, 20);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(page));
        AdminMailInboxController controller = WithContext(new AdminMailInboxController(sender));
        var request = new GetAdminMailInboxMessagePageHttpQuery(2, 10, "to@example.com", "general", Unread: true, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), "subject", "from@example.com", entry.Id);
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetPage(request));
        AdminMailInboxMessagePageHttpResponse response = Assert.IsType<AdminMailInboxMessagePageHttpResponse>(result.Value);
        GetAdminMailInboxMessagePageQuery query = Assert.IsType<GetAdminMailInboxMessagePageQuery>(sender.Request);
        Assert.Multiple(
            () => Assert.Equivalent(page, response, strict: true),
            () => Assert.Equivalent(request, query, strict: true));
    }

    [Fact]
    public async Task Outgoing_ForwardsFiltersAndMapsDeliveryMetadata() {
        var entry = new OutgoingEmailJournalEntry(Guid.NewGuid(), "sent", "welcome", "from@example.com", ["to@example.com"], "subject", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(1), 1, 3, "trace", "body", ContentHidden: false, "reply@example.com", "message-id");
        var page = new OutgoingEmailJournalPage([entry], 31, new Dictionary<string, long>(StringComparer.Ordinal) { ["sent"] = 31 });
        CapturedSender sender = SubstituteSender.Capture(Result.Success(page));
        AdminOutgoingEmailsController controller = WithContext(new AdminOutgoingEmailsController(sender));
        var request = new GetAdminOutgoingEmailsHttpQuery(2, 10, "welcome", "sent", "to@example.com", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), entry.Id, "trace");
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetPage(request));
        AdminOutgoingEmailPageHttpResponse response = Assert.IsType<AdminOutgoingEmailPageHttpResponse>(result.Value);
        GetAdminOutgoingEmailsQuery query = Assert.IsType<GetAdminOutgoingEmailsQuery>(sender.Request);
        Assert.Multiple(
            () => Assert.Equivalent(page, response, strict: true),
            () => Assert.Equivalent(request, query, strict: true));
    }

    [Fact]
    public async Task Retention_MapsCohortsAndDailyActivity() {
        var report = new AdminRetentionReport(DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(2), DateTime.UnixEpoch.AddDays(40), 15,
            [new AdminRetentionCohort(DateTime.UnixEpoch, 10, 8, 7, 6, Day30: null)], [new AdminRetentionDay(DateTime.UnixEpoch, 5)]);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(report));
        AdminRetentionController controller = WithContext(new AdminRetentionController(sender));
        var request = new GetAdminRetentionHttpQuery(new DateOnly(1970, 1, 1), new DateOnly(1970, 1, 2));
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.Get(request));
        AdminRetentionReportHttpResponse response = Assert.IsType<AdminRetentionReportHttpResponse>(result.Value);
        GetAdminRetentionQuery query = Assert.IsType<GetAdminRetentionQuery>(sender.Request);
        Assert.Multiple(
            () => Assert.Equivalent(report, response, strict: true),
            () => Assert.Equivalent(request, query, strict: true));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Revisions_SelectsTemplateKindAndMapsHistory(bool ai) {
        IReadOnlyList<AdminTemplateRevisionModel> revisions = [new AdminTemplateRevisionModel(Guid.NewGuid(), "subject", "html", "text", IsActive: false, 2, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1))];
        CapturedSender sender = SubstituteSender.Capture(Result.Success(revisions));
        IActionResult action = ai
            ? await WithContext(new AdminAiPromptsController(sender)).GetRevisions("welcome", "en")
            : await WithContext(new AdminEmailTemplatesController(sender)).GetRevisions("welcome", "en");
        OkObjectResult result = Assert.IsType<OkObjectResult>(action);
        List<AdminTemplateRevisionHttpResponse> response = Assert.IsType<List<AdminTemplateRevisionHttpResponse>>(result.Value);
        GetAdminTemplateRevisionsQuery query = Assert.IsType<GetAdminTemplateRevisionsQuery>(sender.Request);
        Assert.Multiple(
            () => Assert.Equivalent(revisions, response, strict: true),
            () => Assert.Equal(new GetAdminTemplateRevisionsQuery("welcome", "en", ai), query));
    }

    [Fact]
    public async Task AcquisitionRange_ForwardsFiltersAndMapsDailyComparison() {
        var summary = new FoodDiary.Application.Marketing.Models.MarketingAttributionSummaryModel(24, DateTime.UnixEpoch, 10, 8, 2, 1, 5, 6, 4, 6, 3, 5, 25, 50, LastEventAtUtc: null, [], [], []);
        var report = new FoodDiary.Application.Marketing.Models.MarketingAttributionRangeModel(DateTime.UnixEpoch.AddDays(1), DateTime.UnixEpoch.AddDays(2), DateTime.UnixEpoch,
            summary, summary, [new FoodDiary.Application.Marketing.Models.MarketingAttributionDayModel(DateTime.UnixEpoch.AddDays(1), 8, 2, 1)], 31);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(report));
        AdminAcquisitionController controller = WithContext(new AdminAcquisitionController(sender));
        var request = new GetMarketingAttributionRangeHttpQuery(DateTimeOffset.UnixEpoch.AddDays(1), DateTimeOffset.UnixEpoch.AddDays(2), 2, 10, "page_landing", "tracked", "source");
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetRange(request));
        MarketingAttributionRangeHttpResponse response = Assert.IsType<MarketingAttributionRangeHttpResponse>(result.Value);
        FoodDiary.Application.Marketing.Queries.GetMarketingAttributionRange.GetMarketingAttributionRangeQuery query = Assert.IsType<FoodDiary.Application.Marketing.Queries.GetMarketingAttributionRange.GetMarketingAttributionRangeQuery>(sender.Request);
        Assert.Multiple(() => Assert.Equivalent(report, response, strict: true), () => Assert.Equivalent(request, query, strict: true));
    }

    private static T WithContext<T>(T controller) where T : ControllerBase {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }
}
