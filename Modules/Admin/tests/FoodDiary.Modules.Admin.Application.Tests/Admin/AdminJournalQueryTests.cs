using FoodDiary.Testing;
using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminAudit;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBugReports;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessagePage;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminOutgoingEmails;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminRetention;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminTemplateRevisions;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public class AdminJournalQueryTests {
    [Fact]
    public async Task Audit_MapsEveryFieldAndPreservesTotal() {
        using var cancellation = new CancellationTokenSource();
        IAuditEntryJournal journal = Substitute.For<IAuditEntryJournal>();
        var filter = new AuditEntryFilter(2, 10, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), Guid.NewGuid(), Guid.NewGuid(), "read", "User", "target");
        var entry = new AuditEntryReadModel(Guid.NewGuid(), filter.ActorUserId!.Value, filter.SubjectClientUserId, "read", "User", "target", "metadata", DateTime.UnixEpoch);
        journal.GetPageAsync(filter, cancellation.Token).Returns(new AuditEntryPage([entry], 31));
        Result<AdminAuditPage> result = await new GetAdminAuditQueryHandler(journal).Handle(new GetAdminAuditQuery(filter), cancellation.Token);
        ResultAssert.Success(result);
        AdminAuditEntryModel actual = Assert.Single(result.Value.Items);
        Assert.Multiple(
            () => Assert.Equal(31, result.Value.TotalItems),
            () => Assert.Equal(entry.Id, actual.Id),
            () => Assert.Equal(entry.ActorUserId, actual.ActorUserId),
            () => Assert.Equal(entry.SubjectClientUserId, actual.SubjectClientUserId),
            () => Assert.Equal(entry.Action, actual.Action),
            () => Assert.Equal(entry.TargetType, actual.TargetType),
            () => Assert.Equal(entry.TargetId, actual.TargetId),
            () => Assert.Equal(entry.Metadata, actual.Metadata),
            () => Assert.Equal(entry.CreatedAtUtc, actual.CreatedAtUtc));
        await journal.Received(1).GetPageAsync(filter, cancellation.Token);
    }

    [Fact]
    public async Task BugReports_PreservesFilterAndConfigurationState() {
        using var cancellation = new CancellationTokenSource();
        IAdminBugReportReader reader = Substitute.For<IAdminBugReportReader>();
        var filter = new AdminBugReportFilter(2, 10, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), "processed", "subject", Guid.NewGuid());
        var entry = new AdminBugReportEntry(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UnixEpoch, "subject", "processed", 2, "summary", "https://example.com/pr/1", ContentExpired: true);
        var page = new AdminBugReportPage([entry], 30) { IsConfigured = false };
        reader.GetPageAsync(filter, cancellation.Token).Returns(page);
        Result<AdminBugReportPage> result = await new GetAdminBugReportsQueryHandler(reader).Handle(new GetAdminBugReportsQuery(filter), cancellation.Token);
        ResultAssert.Success(result);
        Assert.Same(page, result.Value);
        Assert.False(result.Value.IsConfigured);
        await reader.Received(1).GetPageAsync(filter, cancellation.Token);
    }

    [Fact]
    public async Task Inbox_PreservesAllFiltersAndPageCounters() {
        using var cancellation = new CancellationTokenSource();
        IAdminMailInboxReader reader = Substitute.For<IAdminMailInboxReader>();
        var query = new GetAdminMailInboxMessagePageQuery(2, 10, "to@example.com", "general", Unread: true, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), "subject", "from@example.com", Guid.NewGuid());
        var page = new AdminMailInboxMessagePageModel([], 30, 12, 18);
        reader.GetMessagePageAsync(query.Page, query.Limit, query.Recipient, query.Category, query.Unread, cancellation.Token, query.FromUtc, query.ToUtc, query.Search, query.FromAddress, query.Id).Returns(page);
        Result<AdminMailInboxMessagePageModel> result = await new GetAdminMailInboxMessagePageQueryHandler(reader).Handle(query, cancellation.Token);
        ResultAssert.Success(result);
        Assert.Same(page, result.Value);
        await reader.Received(1).GetMessagePageAsync(2, 10, query.Recipient, "general", unread: true, cancellation.Token, query.FromUtc, query.ToUtc, "subject", query.FromAddress, query.Id);
    }

    [Fact]
    public async Task Outgoing_PreservesAllFiltersAndResult() {
        using var cancellation = new CancellationTokenSource();
        IOutgoingEmailJournal journal = Substitute.For<IOutgoingEmailJournal>();
        var query = new GetAdminOutgoingEmailsQuery(2, 10, "welcome", "sent", "to@example.com", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), Guid.NewGuid(), "trace");
        var entry = new OutgoingEmailJournalEntry(Guid.NewGuid(), "sent", "welcome", "from@example.com", ["to@example.com"], "subject", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(1), 1, 3, "trace", "body", ContentHidden: false, "reply@example.com", "message-id");
        var page = new OutgoingEmailJournalPage([entry], 30, new Dictionary<string, long>(StringComparer.Ordinal) { ["sent"] = 30 });
        journal.GetPageAsync(2, 10, query.Purpose, query.Status, query.Recipient, cancellation.Token, query.FromUtc, query.ToUtc, query.Id, query.CorrelationId).Returns(page);
        Result<OutgoingEmailJournalPage> result = await new GetAdminOutgoingEmailsQueryHandler(journal).Handle(query, cancellation.Token);
        ResultAssert.Success(result);
        Assert.Same(page, result.Value);
        await journal.Received(1).GetPageAsync(2, 10, "welcome", "sent", query.Recipient, cancellation.Token, query.FromUtc, query.ToUtc, query.Id, "trace");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Revisions_NormalizesKeyAndLocale(bool ai) {
        using var cancellation = new CancellationTokenSource();
        ISender email = Substitute.For<ISender>();
        ISender prompts = Substitute.For<ISender>();
        var id = Guid.NewGuid();
        email.Send(new GetEmailTemplateRevisionsQuery(Key: "welcome", Locale: "en"), cancellation.Token).Returns([new EmailTemplateRevisionReadModel(id, "subject", "html", "text", IsActive: true, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1))]);
        prompts.Send(new GetAiPromptRevisionsQuery(Key: "welcome", Locale: "en"), cancellation.Token).Returns([new AiPromptRevisionReadModel(id, "text", 2, IsActive: true, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1))]);
        Result<IReadOnlyList<AdminTemplateRevisionModel>> result = await new GetAdminTemplateRevisionsQueryHandler(RequestTestSender.Route((email, [typeof(global::FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions.GetEmailTemplateRevisionsQuery)]), (prompts, [typeof(global::FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQuery)]))).Handle(new GetAdminTemplateRevisionsQuery(" WELCOME ", " EN ", ai), cancellation.Token);
        ResultAssert.Success(result);
        Assert.Equal(id, Assert.Single(result.Value).Id);
        if (ai) {
            await prompts.Received(1).Send(new GetAiPromptRevisionsQuery(Key: "welcome", Locale: "en"), cancellation.Token);
            Assert.Empty(email.ReceivedCalls());
        } else {
            await email.Received(1).Send(new GetEmailTemplateRevisionsQuery(Key: "welcome", Locale: "en"), cancellation.Token);
            Assert.Empty(prompts.ReceivedCalls());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retention_UsesUtcInclusiveDatesAndExclusiveEnd(bool explicitDates) {
        using var cancellation = new CancellationTokenSource();
        var now = new DateTimeOffset(2026, 9, 9, 12, 34, 56, TimeSpan.Zero);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        IAdminRetentionReader reader = Substitute.For<IAdminRetentionReader>();
        DateOnly from = explicitDates ? new DateOnly(2026, 9, 1) : new DateOnly(2026, 8, 11);
        DateOnly to = explicitDates ? new DateOnly(2026, 9, 3) : new DateOnly(2026, 9, 9);
        var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var report = new AdminRetentionReport(start, end, now.UtcDateTime, 4, [], []);
        reader.GetAsync(start, end, now.UtcDateTime, cancellation.Token).Returns(report);
        Result<AdminRetentionReport> result = await new GetAdminRetentionQueryHandler(reader, clock).Handle(new GetAdminRetentionQuery(explicitDates ? from : null, explicitDates ? to : null), cancellation.Token);
        ResultAssert.Success(result);
        Assert.Same(report, result.Value);
        await reader.Received(1).GetAsync(start, end, now.UtcDateTime, cancellation.Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retention_PreservesIndependentCohortDates(bool explicitEnd) {
        var now = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(now);
        IAdminRetentionReader reader = Substitute.For<IAdminRetentionReader>();
        var from = new DateOnly(2026, 9, 1);
        var cohortFrom = new DateOnly(2026, 6, 1);
        var cohortTo = new DateOnly(2026, 7, 31);
        await new GetAdminRetentionQueryHandler(reader, clock).Handle(
            new GetAdminRetentionQuery(from, from, cohortFrom, explicitEnd ? cohortTo : null), CancellationToken.None);
        await reader.Received(1).GetAsync(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            from.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), now.UtcDateTime, CancellationToken.None,
            cohortFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), (explicitEnd ? cohortTo : DateOnly.FromDateTime(now.UtcDateTime)).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("2026-09-09", "2026-09-08")]
    [InlineData("2026-09-09", "2026-09-10")]
    [InlineData("1969-12-31", "2026-09-09")]
    public async Task Retention_InvalidCohortRangeDoesNotRead(string from, string to) {
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));
        IAdminRetentionReader reader = Substitute.For<IAdminRetentionReader>();
        Result<AdminRetentionReport> result = await new GetAdminRetentionQueryHandler(reader, clock).Handle(
            new GetAdminRetentionQuery(From: null, To: null, DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture),
                DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture)), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Theory]
    [InlineData("2026-09-09", "2026-09-08")]
    [InlineData("2026-09-09", "2026-09-10")]
    [InlineData("1969-12-31", "2026-09-09")]
    public async Task Retention_InvalidRangeDoesNotRead(string from, string to) {
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));
        IAdminRetentionReader reader = Substitute.For<IAdminRetentionReader>();
        Result<AdminRetentionReport> result = await new GetAdminRetentionQueryHandler(reader, clock).Handle(new GetAdminRetentionQuery(DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture), DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture)), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(reader.ReceivedCalls());
    }
}
