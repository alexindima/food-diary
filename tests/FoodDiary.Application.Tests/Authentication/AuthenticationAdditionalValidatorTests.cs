using FluentValidation.TestHelper;
using FoodDiary.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Authentication.Commands.GoogleLogin;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Authentication.Commands.TelegramVerify;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class AuthenticationAdditionalValidatorTests {
    // â”€â”€ AdminSsoExchange â”€â”€

    [Fact]
    public async Task AdminSsoExchange_WithEmptyCode_HasError() {
        var validator = new AdminSsoExchangeCommandValidator();
        TestValidationResult<AdminSsoExchangeCommand> result = await validator.TestValidateAsync(new AdminSsoExchangeCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.Code);
    }

    [Fact]
    public async Task AdminSsoExchange_WithValidCode_NoErrors() {
        var validator = new AdminSsoExchangeCommandValidator();
        TestValidationResult<AdminSsoExchangeCommand> result = await validator.TestValidateAsync(new AdminSsoExchangeCommand("valid-code"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ GoogleLogin â”€â”€

    [Fact]
    public async Task GoogleLogin_WithEmptyCredential_HasError() {
        var validator = new GoogleLoginCommandValidator();
        TestValidationResult<GoogleLoginCommand> result = await validator.TestValidateAsync(new GoogleLoginCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.Credential);
    }

    [Fact]
    public async Task GoogleLogin_WithValidCredential_NoErrors() {
        var validator = new GoogleLoginCommandValidator();
        TestValidationResult<GoogleLoginCommand> result = await validator.TestValidateAsync(new GoogleLoginCommand("id-token"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ LinkTelegram â”€â”€

    [Fact]
    public async Task LinkTelegram_WithEmptyUserId_HasError() {
        var validator = new LinkTelegramCommandValidator();
        TestValidationResult<LinkTelegramCommand> result = await validator.TestValidateAsync(new LinkTelegramCommand(Guid.Empty, "data"));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task LinkTelegram_WithEmptyInitData_HasError() {
        var validator = new LinkTelegramCommandValidator();
        TestValidationResult<LinkTelegramCommand> result = await validator.TestValidateAsync(new LinkTelegramCommand(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(c => c.InitData);
    }

    [Fact]
    public async Task LinkTelegram_WithValidData_NoErrors() {
        var validator = new LinkTelegramCommandValidator();
        TestValidationResult<LinkTelegramCommand> result = await validator.TestValidateAsync(new LinkTelegramCommand(Guid.NewGuid(), "init-data"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ Login â”€â”€

    [Fact]
    public async Task Login_WithEmptyEmail_HasError() {
        var validator = new LoginCommandValidator();
        TestValidationResult<LoginCommand> result = await validator.TestValidateAsync(new LoginCommand("", "pass"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_HasError() {
        var validator = new LoginCommandValidator();
        TestValidationResult<LoginCommand> result = await validator.TestValidateAsync(new LoginCommand("not-email", "pass"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_HasError() {
        var validator = new LoginCommandValidator();
        TestValidationResult<LoginCommand> result = await validator.TestValidateAsync(new LoginCommand("user@test.com", ""));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task Login_WithValidData_NoErrors() {
        var validator = new LoginCommandValidator();
        TestValidationResult<LoginCommand> result = await validator.TestValidateAsync(new LoginCommand("user@test.com", "password"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ RefreshToken â”€â”€

    [Fact]
    public async Task RefreshToken_WithEmptyToken_HasError() {
        var validator = new RefreshTokenCommandValidator();
        TestValidationResult<RefreshTokenCommand> result = await validator.TestValidateAsync(new RefreshTokenCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_NoErrors() {
        var validator = new RefreshTokenCommandValidator();
        TestValidationResult<RefreshTokenCommand> result = await validator.TestValidateAsync(new RefreshTokenCommand("token-value"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ RestoreAccount â”€â”€

    [Fact]
    public async Task RestoreAccount_WithEmptyEmail_HasError() {
        var validator = new RestoreAccountCommandValidator();
        TestValidationResult<RestoreAccountCommand> result = await validator.TestValidateAsync(new RestoreAccountCommand("", "password1"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public async Task RestoreAccount_WithShortPassword_HasError() {
        var validator = new RestoreAccountCommandValidator();
        TestValidationResult<RestoreAccountCommand> result = await validator.TestValidateAsync(new RestoreAccountCommand("user@test.com", "12345"));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public async Task RestoreAccount_WithValidData_NoErrors() {
        var validator = new RestoreAccountCommandValidator();
        TestValidationResult<RestoreAccountCommand> result = await validator.TestValidateAsync(new RestoreAccountCommand("user@test.com", "password1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ TelegramVerify â”€â”€

    [Fact]
    public async Task TelegramVerify_WithEmptyInitData_HasError() {
        var validator = new TelegramVerifyCommandValidator();
        TestValidationResult<TelegramVerifyCommand> result = await validator.TestValidateAsync(new TelegramVerifyCommand(""));
        result.ShouldHaveValidationErrorFor(c => c.InitData);
    }

    [Fact]
    public async Task TelegramVerify_WithValidInitData_NoErrors() {
        var validator = new TelegramVerifyCommandValidator();
        TestValidationResult<TelegramVerifyCommand> result = await validator.TestValidateAsync(new TelegramVerifyCommand("init-data"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
