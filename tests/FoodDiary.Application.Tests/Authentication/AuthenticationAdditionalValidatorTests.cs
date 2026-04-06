using FluentValidation.TestHelper;
using FoodDiary.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Authentication.Commands.GoogleLogin;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Authentication.Commands.TelegramVerify;

namespace FoodDiary.Application.Tests.Authentication;

public class AuthenticationAdditionalValidatorTests {
    // ── AdminSsoExchange ──

    [Fact]
    public async Task AdminSsoExchange_WithEmptyCode_HasError() {
        var validator = new AdminSsoExchangeCommandValidator();
        var result = await validator.TestValidateAsync(new AdminSsoExchangeCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.Code);
    }

    [Fact]
    public async Task AdminSsoExchange_WithValidCode_NoErrors() {
        var validator = new AdminSsoExchangeCommandValidator();
        var result = await validator.TestValidateAsync(new AdminSsoExchangeCommand("valid-code"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── GoogleLogin ──

    [Fact]
    public async Task GoogleLogin_WithEmptyCredential_HasError() {
        var validator = new GoogleLoginCommandValidator();
        var result = await validator.TestValidateAsync(new GoogleLoginCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.Credential);
    }

    [Fact]
    public async Task GoogleLogin_WithValidCredential_NoErrors() {
        var validator = new GoogleLoginCommandValidator();
        var result = await validator.TestValidateAsync(new GoogleLoginCommand("id-token"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── LinkTelegram ──

    [Fact]
    public async Task LinkTelegram_WithEmptyUserId_HasError() {
        var validator = new LinkTelegramCommandValidator();
        var result = await validator.TestValidateAsync(new LinkTelegramCommand(Guid.Empty, "data"));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task LinkTelegram_WithEmptyInitData_HasError() {
        var validator = new LinkTelegramCommandValidator();
        var result = await validator.TestValidateAsync(new LinkTelegramCommand(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(c => c.InitData);
    }

    [Fact]
    public async Task LinkTelegram_WithValidData_NoErrors() {
        var validator = new LinkTelegramCommandValidator();
        var result = await validator.TestValidateAsync(new LinkTelegramCommand(Guid.NewGuid(), "init-data"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── Login ──

    [Fact]
    public async Task Login_WithEmptyEmail_HasError() {
        var validator = new LoginCommandValidator();
        var result = await validator.TestValidateAsync(new LoginCommand("", "pass"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_HasError() {
        var validator = new LoginCommandValidator();
        var result = await validator.TestValidateAsync(new LoginCommand("not-email", "pass"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_HasError() {
        var validator = new LoginCommandValidator();
        var result = await validator.TestValidateAsync(new LoginCommand("user@test.com", ""));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Login_WithValidData_NoErrors() {
        var validator = new LoginCommandValidator();
        var result = await validator.TestValidateAsync(new LoginCommand("user@test.com", "password"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── RefreshToken ──

    [Fact]
    public async Task RefreshToken_WithEmptyToken_HasError() {
        var validator = new RefreshTokenCommandValidator();
        var result = await validator.TestValidateAsync(new RefreshTokenCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_NoErrors() {
        var validator = new RefreshTokenCommandValidator();
        var result = await validator.TestValidateAsync(new RefreshTokenCommand("token-value"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── RestoreAccount ──

    [Fact]
    public async Task RestoreAccount_WithEmptyEmail_HasError() {
        var validator = new RestoreAccountCommandValidator();
        var result = await validator.TestValidateAsync(new RestoreAccountCommand("", "password1"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task RestoreAccount_WithShortPassword_HasError() {
        var validator = new RestoreAccountCommandValidator();
        var result = await validator.TestValidateAsync(new RestoreAccountCommand("user@test.com", "12345"));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task RestoreAccount_WithValidData_NoErrors() {
        var validator = new RestoreAccountCommandValidator();
        var result = await validator.TestValidateAsync(new RestoreAccountCommand("user@test.com", "password1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── TelegramVerify ──

    [Fact]
    public async Task TelegramVerify_WithEmptyInitData_HasError() {
        var validator = new TelegramVerifyCommandValidator();
        var result = await validator.TestValidateAsync(new TelegramVerifyCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.InitData);
    }

    [Fact]
    public async Task TelegramVerify_WithValidInitData_NoErrors() {
        var validator = new TelegramVerifyCommandValidator();
        var result = await validator.TestValidateAsync(new TelegramVerifyCommand("init-data"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
