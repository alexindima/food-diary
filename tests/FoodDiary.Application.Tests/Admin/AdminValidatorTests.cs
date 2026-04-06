using FluentValidation.TestHelper;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;

namespace FoodDiary.Application.Tests.Admin;

public class AdminValidatorTests {
    // ── UpdateAdminUser (Guid UserId, bool? IsActive, bool? IsEmailConfirmed, IReadOnlyList<string>? Roles, string? Language, long? AiInputTokenLimit, long? AiOutputTokenLimit) ──

    [Fact]
    public async Task UpdateAdminUser_WithEmptyUserId_HasError() {
        var result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.Empty, null, null, null, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateAdminUser_WithNegativeAiTokenLimit_HasError() {
        var result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, null, null, -1, null));
        result.ShouldHaveValidationErrorFor(c => c.AiInputTokenLimit);
    }

    [Fact]
    public async Task UpdateAdminUser_WithInvalidLanguage_HasError() {
        var result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, null, "invalid-lang", null, null));
        result.ShouldHaveValidationErrorFor(c => c.Language);
    }

    [Fact]
    public async Task UpdateAdminUser_WithUnknownRole_HasError() {
        var result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, new[] { "UnknownRole" }, null, null, null));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpdateAdminUser_WithValidAdminRole_NoErrors() {
        var result = await new UpdateAdminUserCommandValidator().TestValidateAsync(
            new UpdateAdminUserCommand(Guid.NewGuid(), null, null, new[] { "Admin" }, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── UpsertAdminEmailTemplate (string Key, string Locale, string Subject, string HtmlBody, string TextBody, bool IsActive) ──

    [Fact]
    public async Task UpsertEmailTemplate_WithEmptyKey_HasError() {
        var result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("", "en", "Subject", "<p>Body</p>", "Body", true));
        result.ShouldHaveValidationErrorFor(c => c.Key);
    }

    [Fact]
    public async Task UpsertEmailTemplate_WithInvalidLocale_HasError() {
        var result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("key", "xx", "Subject", "<p>Body</p>", "Body", true));
        result.ShouldHaveValidationErrorFor(c => c.Locale);
    }

    [Fact]
    public async Task UpsertEmailTemplate_WithEmptySubject_HasError() {
        var result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("key", "en", "", "<p>Body</p>", "Body", true));
        result.ShouldHaveValidationErrorFor(c => c.Subject);
    }

    [Fact]
    public async Task UpsertEmailTemplate_WithValidData_NoErrors() {
        var result = await new UpsertAdminEmailTemplateCommandValidator().TestValidateAsync(
            new UpsertAdminEmailTemplateCommand("welcome", "en", "Welcome!", "<p>Hi</p>", "Hi", true));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── GetAdminAiUsageSummary (DateOnly?, DateOnly?) ──

    [Fact]
    public async Task GetAdminAiUsage_WithInvertedDates_HasError() {
        var result = await new GetAdminAiUsageSummaryQueryValidator().TestValidateAsync(
            new GetAdminAiUsageSummaryQuery(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7))));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetAdminAiUsage_WithNullDates_NoErrors() {
        var result = await new GetAdminAiUsageSummaryQueryValidator().TestValidateAsync(
            new GetAdminAiUsageSummaryQuery(null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
