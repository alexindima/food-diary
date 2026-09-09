using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Admin.Queries.GetAdminAudit;
using FoodDiary.Application.Admin.Queries.GetAdminBugReports;
using FoodDiary.Application.Admin.Queries.GetAdminContentReports;
using FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessagePage;
using FoodDiary.Application.Admin.Queries.GetAdminOutgoingEmails;
using FoodDiary.Application.Admin.Queries.GetAdminTemplateRevisions;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public class AdminJournalValidatorTests {
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(0, null, true)]
    [InlineData(null, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    public void Journals_RequireExclusiveEndAfterStart(int? start, int? end, bool valid) {
        DateTimeOffset? from = start.HasValue ? DateTimeOffset.UnixEpoch.AddDays(start.Value) : null;
        DateTimeOffset? to = end.HasValue ? DateTimeOffset.UnixEpoch.AddDays(end.Value) : null;
        Assert.Multiple(
            () => Assert.Equal(valid, new GetAdminAuditQueryValidator().Validate(new GetAdminAuditQuery(new AuditEntryFilter(1, 50, from, to, ActorUserId: null, SubjectClientUserId: null, Action: null, TargetType: null, TargetId: null))).IsValid),
            () => Assert.Equal(valid, new GetAdminBugReportsQueryValidator().Validate(new GetAdminBugReportsQuery(new AdminBugReportFilter(1, 50, from, to, Status: null, Search: null, Id: null))).IsValid),
            () => Assert.Equal(valid, new GetAdminImpersonationSessionsQueryValidator().Validate(new GetAdminImpersonationSessionsQuery(1, 50, Search: null, from, to)).IsValid),
            () => Assert.Equal(valid, new GetAdminMailInboxMessagePageQueryValidator().Validate(new GetAdminMailInboxMessagePageQuery(FromUtc: from, ToUtc: to)).IsValid),
            () => Assert.Equal(valid, new GetAdminOutgoingEmailsQueryValidator().Validate(new GetAdminOutgoingEmailsQuery(FromUtc: from, ToUtc: to)).IsValid),
            () => Assert.Equal(valid, new GetAdminUserLoginEventsQueryValidator().Validate(new GetAdminUserLoginEventsQuery(1, 50, UserId: null, Search: null, from, to)).IsValid),
            () => Assert.Equal(valid, new GetAdminContentReportsQueryValidator().Validate(new GetAdminContentReportsQuery(Status: null, 1, 50, from?.UtcDateTime, to?.UtcDateTime)).IsValid));
    }

    [Fact]
    public void Audit_RejectsInvalidPagingAndOversizedFilters() {
        TestValidationResult<GetAdminAuditQuery> result = new GetAdminAuditQueryValidator().TestValidate(new GetAdminAuditQuery(new AuditEntryFilter(0, 101, FromUtc: null, ToUtc: null, ActorUserId: null, SubjectClientUserId: null, new string('a', 201), new string('t', 101), new string('i', 201))));
        result.ShouldHaveValidationErrorFor(x => x.Filter.Page);
        result.ShouldHaveValidationErrorFor(x => x.Filter.Limit);
        result.ShouldHaveValidationErrorFor(x => x.Filter.Action);
        result.ShouldHaveValidationErrorFor(x => x.Filter.TargetType);
        result.ShouldHaveValidationErrorFor(x => x.Filter.TargetId);
    }

    [Fact]
    public void BugReports_RejectsInvalidPagingAndOversizedFilters() {
        TestValidationResult<GetAdminBugReportsQuery> result = new GetAdminBugReportsQueryValidator().TestValidate(new GetAdminBugReportsQuery(new AdminBugReportFilter(10001, 0, FromUtc: null, ToUtc: null, new string('s', 33), new string('q', 321), Id: null)));
        result.ShouldHaveValidationErrorFor(x => x.Filter.Page);
        result.ShouldHaveValidationErrorFor(x => x.Filter.Limit);
        result.ShouldHaveValidationErrorFor(x => x.Filter.Status);
        result.ShouldHaveValidationErrorFor(x => x.Filter.Search);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("general", true)]
    [InlineData("dmarc-report", true)]
    [InlineData("other", false)]
    public void Inbox_ValidatesCategory(string? category, bool valid) {
        Assert.Equal(valid, new GetAdminMailInboxMessagePageQueryValidator().Validate(new GetAdminMailInboxMessagePageQuery(Category: category)).IsValid);
    }

    [Fact]
    public void Inbox_RejectsInvalidPagingAndOversizedFilters() {
        TestValidationResult<GetAdminMailInboxMessagePageQuery> result = new GetAdminMailInboxMessagePageQueryValidator().TestValidate(new GetAdminMailInboxMessagePageQuery(0, 201, new string('r', 321), Search: new string('s', 321), FromAddress: new string('f', 321)));
        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.Limit);
        result.ShouldHaveValidationErrorFor(x => x.Recipient);
        result.ShouldHaveValidationErrorFor(x => x.Search);
        result.ShouldHaveValidationErrorFor(x => x.FromAddress);
    }

    [Fact]
    public void Outgoing_RejectsInvalidPagingAndOversizedFilters() {
        TestValidationResult<GetAdminOutgoingEmailsQuery> result = new GetAdminOutgoingEmailsQueryValidator().TestValidate(new GetAdminOutgoingEmailsQuery(0, 101, new string('p', 65), new string('s', 33), new string('r', 321), CorrelationId: new string('c', 257)));
        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.Limit);
        result.ShouldHaveValidationErrorFor(x => x.Purpose);
        result.ShouldHaveValidationErrorFor(x => x.Status);
        result.ShouldHaveValidationErrorFor(x => x.Recipient);
        result.ShouldHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Recipe", true)]
    [InlineData("Comment", true)]
    [InlineData("User", false)]
    public void ContentReports_ValidatesTargetType(string? targetType, bool valid) {
        Assert.Equal(valid, new GetAdminContentReportsQueryValidator().Validate(new GetAdminContentReportsQuery(Status: null, 1, 50, TargetType: targetType)).IsValid);
    }

    [Fact]
    public void ContentReports_RejectsEmptyIdentifiers() {
        TestValidationResult<GetAdminContentReportsQuery> result = new GetAdminContentReportsQueryValidator().TestValidate(new GetAdminContentReportsQuery(Status: null, 1, 50, ReporterId: Guid.Empty, TargetId: Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ReporterId);
        result.ShouldHaveValidationErrorFor(x => x.TargetId);
    }

    [Fact]
    public void LoginEvents_RejectsOversizedProviderAndDevice() {
        TestValidationResult<GetAdminUserLoginEventsQuery> result = new GetAdminUserLoginEventsQueryValidator().TestValidate(new GetAdminUserLoginEventsQuery(1, 50, UserId: null, Search: null, Provider: new string('p', 101), Device: new string('d', 101)));
        result.ShouldHaveValidationErrorFor(x => x.Provider);
        result.ShouldHaveValidationErrorFor(x => x.Device);
    }

    [Theory]
    [InlineData("welcome", "en", true)]
    [InlineData("", "en", false)]
    [InlineData("welcome", "", false)]
    public void Revisions_RequireKeyAndLocale(string key, string locale, bool valid) {
        Assert.Equal(valid, new GetAdminTemplateRevisionsQueryValidator().Validate(new GetAdminTemplateRevisionsQuery(key, locale, IsAiPrompt: false)).IsValid);
    }

    [Fact]
    public void Revisions_RejectsOversizedKeyAndLocale() {
        TestValidationResult<GetAdminTemplateRevisionsQuery> result = new GetAdminTemplateRevisionsQueryValidator().TestValidate(new GetAdminTemplateRevisionsQuery(new string('k', 65), new string('l', 11), IsAiPrompt: true));
        result.ShouldHaveValidationErrorFor(x => x.Key);
        result.ShouldHaveValidationErrorFor(x => x.Locale);
    }
}
