using FluentValidation.TestHelper;
using FoodDiary.Application.Admin.Commands.StartAdminImpersonation;
using FoodDiary.Application.Admin.Commands.ImportAdminLessons;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public class AdminValidatorTests {
    [Fact]
    public async Task StartAdminImpersonation_WithEmptyActorUserId_HasError() {
        TestValidationResult<StartAdminImpersonationCommand> result = await new StartAdminImpersonationCommandValidator().TestValidateAsync(
            new StartAdminImpersonationCommand(Guid.Empty, Guid.NewGuid(), "Valid support reason", null, null));

        result.ShouldHaveValidationErrorFor(command => command.ActorUserId);
    }

    [Fact]
    public async Task StartAdminImpersonation_WithEmptyTargetUserId_HasError() {
        TestValidationResult<StartAdminImpersonationCommand> result = await new StartAdminImpersonationCommandValidator().TestValidateAsync(
            new StartAdminImpersonationCommand(Guid.NewGuid(), Guid.Empty, "Valid support reason", null, null));

        result.ShouldHaveValidationErrorFor(command => command.TargetUserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task StartAdminImpersonation_WithInvalidReason_HasError(string reason) {
        TestValidationResult<StartAdminImpersonationCommand> result = await new StartAdminImpersonationCommandValidator().TestValidateAsync(
            new StartAdminImpersonationCommand(Guid.NewGuid(), Guid.NewGuid(), reason, null, null));

        result.ShouldHaveValidationErrorFor(command => command.Reason);
    }

    [Fact]
    public async Task StartAdminImpersonation_WithValidData_NoErrors() {
        TestValidationResult<StartAdminImpersonationCommand> result = await new StartAdminImpersonationCommandValidator().TestValidateAsync(
            new StartAdminImpersonationCommand(Guid.NewGuid(), Guid.NewGuid(), "Investigating support ticket", null, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ UpdateAdminUser (Guid UserId, bool? IsActive, bool? IsEmailConfirmed, IReadOnlyList<string>? Roles, string? Language, long? AiInputTokenLimit, long? AiOutputTokenLimit) â”€â”€

    [Fact]
    public async Task UpdateAdminUser_WithEmptyUserId_HasError() {
        TestValidationResult<UpdateAdminUserCommand> result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.Empty, null, null, null, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateAdminUser_WithNegativeAiTokenLimit_HasError() {
        TestValidationResult<UpdateAdminUserCommand> result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, null, null, -1, null));
        result.ShouldHaveValidationErrorFor(c => c.AiInputTokenLimit);
    }

    [Fact]
    public async Task UpdateAdminUser_WithInvalidLanguage_HasError() {
        TestValidationResult<UpdateAdminUserCommand> result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, null, "invalid-lang", null, null));
        result.ShouldHaveValidationErrorFor(c => c.Language);
    }

    [Fact]
    public async Task UpdateAdminUser_WithUnknownRole_HasError() {
        TestValidationResult<UpdateAdminUserCommand> result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, new[] { "UnknownRole" }, null, null, null));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpdateAdminUser_WithValidAdminRole_NoErrors() {
        TestValidationResult<UpdateAdminUserCommand> result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, new[] { "Admin" }, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateAdminUser_WithValidOwnerRole_NoErrors() {
        TestValidationResult<UpdateAdminUserCommand> result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, new[] { "Owner", "Admin" }, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ UpsertAdminEmailTemplate (string Key, string Locale, string Subject, string HtmlBody, string TextBody, bool IsActive) â”€â”€

    [Fact]
    public async Task UpsertEmailTemplate_WithEmptyKey_HasError() {
        TestValidationResult<UpsertAdminEmailTemplateCommand> result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("", "en", "Subject", "<p>Body</p>", "Body", true));
        result.ShouldHaveValidationErrorFor(c => c.Key);
    }

    [Fact]
    public async Task UpsertEmailTemplate_WithInvalidLocale_HasError() {
        TestValidationResult<UpsertAdminEmailTemplateCommand> result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("key", "xx", "Subject", "<p>Body</p>", "Body", true));
        result.ShouldHaveValidationErrorFor(c => c.Locale);
    }

    [Fact]
    public async Task UpsertEmailTemplate_WithEmptySubject_HasError() {
        TestValidationResult<UpsertAdminEmailTemplateCommand> result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("key", "en", "", "<p>Body</p>", "Body", true));
        result.ShouldHaveValidationErrorFor(c => c.Subject);
    }

    [Fact]
    public async Task UpsertEmailTemplate_WithValidData_NoErrors() {
        TestValidationResult<UpsertAdminEmailTemplateCommand> result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("welcome", "en", "Welcome!", "<p>Hi</p>", "Hi", true));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ GetAdminAiUsageSummary (DateOnly?, DateOnly?) â”€â”€

    [Fact]
    public async Task GetAdminAiUsage_WithInvertedDates_HasError() {
        TestValidationResult<GetAdminAiUsageSummaryQuery> result = await new GetAdminAiUsageSummaryQueryValidator().TestValidateAsync(
            new GetAdminAiUsageSummaryQuery(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7))));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetAdminAiUsage_WithNullDates_NoErrors() {
        TestValidationResult<GetAdminAiUsageSummaryQuery> result = await new GetAdminAiUsageSummaryQueryValidator().TestValidateAsync(
            new GetAdminAiUsageSummaryQuery(null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetAdminMailInboxMessages_WithInvalidLimit_HasError(int limit) {
        TestValidationResult<GetAdminMailInboxMessagesQuery> result = await new GetAdminMailInboxMessagesQueryValidator().TestValidateAsync(
            new GetAdminMailInboxMessagesQuery(limit));

        result.ShouldHaveValidationErrorFor(query => query.Limit);
    }

    [Fact]
    public async Task GetAdminMailInboxMessages_WithValidLimit_NoErrors() {
        TestValidationResult<GetAdminMailInboxMessagesQuery> result = await new GetAdminMailInboxMessagesQueryValidator().TestValidateAsync(
            new GetAdminMailInboxMessagesQuery(50));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GetAdminMailInboxMessageDetails_WithEmptyId_HasError() {
        TestValidationResult<GetAdminMailInboxMessageDetailsQuery> result = await new GetAdminMailInboxMessageDetailsQueryValidator().TestValidateAsync(
            new GetAdminMailInboxMessageDetailsQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(query => query.Id);
    }

    [Fact]
    public async Task GetAdminMailInboxMessageDetails_WithValidId_NoErrors() {
        TestValidationResult<GetAdminMailInboxMessageDetailsQuery> result = await new GetAdminMailInboxMessageDetailsQueryValidator().TestValidateAsync(
            new GetAdminMailInboxMessageDetailsQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ImportAdminLessons_WithEmptyLessons_HasError() {
        TestValidationResult<ImportAdminLessonsCommand> result = await new ImportAdminLessonsCommandValidator().TestValidateAsync(
            new ImportAdminLessonsCommand(1, []));

        result.ShouldHaveValidationErrorFor(command => command.Lessons);
    }

    [Fact]
    public async Task ImportAdminLessons_WithUnsupportedVersion_HasError() {
        TestValidationResult<ImportAdminLessonsCommand> result = await new ImportAdminLessonsCommandValidator().TestValidateAsync(
            new ImportAdminLessonsCommand(2, [ValidLesson()]));

        result.ShouldHaveValidationErrorFor(command => command.Version)
            .WithErrorCode("Validation.Invalid");
    }

    [Fact]
    public async Task ImportAdminLessons_WithTooManyLessons_HasError() {
        ImportAdminLessonItem[] lessons = Enumerable.Range(0, 101)
            .Select(index => ValidLesson(title: $"Lesson {index}"))
            .ToArray();

        TestValidationResult<ImportAdminLessonsCommand> result = await new ImportAdminLessonsCommandValidator().TestValidateAsync(
            new ImportAdminLessonsCommand(1, lessons));

        result.ShouldHaveValidationErrorFor(command => command.Lessons)
            .WithErrorCode("Validation.Invalid");
    }

    [Fact]
    public async Task ImportAdminLessons_WithInvalidLessonFields_HasExpectedErrors() {
        TestValidationResult<ImportAdminLessonsCommand> result = await new ImportAdminLessonsCommandValidator().TestValidateAsync(
            new ImportAdminLessonsCommand(1, [
                new ImportAdminLessonItem(
                    Title: "",
                    Content: "",
                    Summary: new string('s', 513),
                    Locale: "",
                    Category: "",
                    Difficulty: "",
                    EstimatedReadMinutes: 0,
                    SortOrder: -1)
            ]));

        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].Title", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].Content", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].Summary", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].Locale", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].Category", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].Difficulty", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].EstimatedReadMinutes", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Lessons[0].SortOrder", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ImportAdminLessons_WithValidLesson_HasNoErrors() {
        TestValidationResult<ImportAdminLessonsCommand> result = await new ImportAdminLessonsCommandValidator().TestValidateAsync(
            new ImportAdminLessonsCommand(1, [ValidLesson()]));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task SendAdminEmailTemplateTest_WithInvalidEmail_HasError() {
        TestValidationResult<SendAdminEmailTemplateTestCommand> result = await new SendAdminEmailTemplateTestCommandValidator().TestValidateAsync(
            new SendAdminEmailTemplateTestCommand("not-email", "welcome", "Subject", "<p>Hi</p>", "Hi"));

        result.ShouldHaveValidationErrorFor(c => c.ToEmail);
    }

    [Fact]
    public async Task SendAdminEmailTemplateTest_WithEmptyTemplateFields_HasErrors() {
        TestValidationResult<SendAdminEmailTemplateTestCommand> result = await new SendAdminEmailTemplateTestCommandValidator().TestValidateAsync(
            new SendAdminEmailTemplateTestCommand("admin@example.com", "", "", "", ""));

        result.ShouldHaveValidationErrorFor(c => c.Key);
        result.ShouldHaveValidationErrorFor(c => c.Subject);
        result.ShouldHaveValidationErrorFor(c => c.HtmlBody);
        result.ShouldHaveValidationErrorFor(c => c.TextBody);
    }

    [Fact]
    public async Task SendAdminEmailTemplateTest_WithValidData_HasNoErrors() {
        TestValidationResult<SendAdminEmailTemplateTestCommand> result = await new SendAdminEmailTemplateTestCommandValidator().TestValidateAsync(
            new SendAdminEmailTemplateTestCommand("admin@example.com", "welcome", "Subject", "<p>Hi</p>", "Hi"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static ImportAdminLessonItem ValidLesson(string title = "Lesson") =>
        new(
            title,
            Content: "Body",
            Summary: "Summary",
            Locale: "en",
            Category: "basics",
            Difficulty: "easy",
            EstimatedReadMinutes: 3,
            SortOrder: 0);
}
