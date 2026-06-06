using FluentValidation.Results;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Authentication.Commands.RequestPasswordReset;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Commands.TelegramBotAuth;
using FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;
using FoodDiary.Application.Authentication.Commands.VerifyEmail;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class AuthenticationValidatorsTests {
    [Fact]
    public async Task AdminSsoStartValidator_WithEmptyUserId_Fails() {
        var validator = new AdminSsoStartCommandValidator();
        var command = new AdminSsoStartCommand(Guid.Empty);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AdminSsoStartValidator_WithValidUserId_Passes() {
        var validator = new AdminSsoStartCommandValidator();
        var command = new AdminSsoStartCommand(Guid.NewGuid());

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ResendEmailVerificationValidator_WithEmptyUserId_Fails() {
        var validator = new ResendEmailVerificationCommandValidator();
        var command = new ResendEmailVerificationCommand(Guid.Empty);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyEmailValidator_WithEmptyToken_Fails() {
        var validator = new VerifyEmailCommandValidator();
        var command = new VerifyEmailCommand(Guid.NewGuid(), string.Empty);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ConfirmPasswordResetValidator_WithShortPassword_Fails() {
        var validator = new ConfirmPasswordResetCommandValidator();
        var command = new ConfirmPasswordResetCommand(Guid.NewGuid(), "token", "12345");

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RequestPasswordResetValidator_WithInvalidEmail_Fails() {
        var validator = new RequestPasswordResetCommandValidator();
        var command = new RequestPasswordResetCommand("not-an-email");

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TelegramBotAuthValidator_WithNonPositiveUserId_Fails() {
        var validator = new TelegramBotAuthCommandValidator();
        var command = new TelegramBotAuthCommand(0);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task TelegramLoginWidgetValidator_WithMissingHash_Fails() {
        var validator = new TelegramLoginWidgetCommandValidator();
        var command = new TelegramLoginWidgetCommand(1, 1, "", Username: null, FirstName: null, LastName: null, PhotoUrl: null);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }
}
