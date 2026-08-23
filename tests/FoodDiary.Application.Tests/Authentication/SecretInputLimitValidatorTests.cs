using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Admin.Commands.CreateAdminUser;
using FoodDiary.Application.Admin.Commands.SetAdminUserPassword;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportCycle;
using FoodDiary.Application.Identity.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Identity.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Identity.Authentication.Commands.GoogleLogin;
using FoodDiary.Application.Identity.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Identity.Authentication.Commands.Login;
using FoodDiary.Application.Identity.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Identity.Authentication.Commands.Register;
using FoodDiary.Application.Identity.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Identity.Authentication.Commands.TelegramLoginWidget;
using FoodDiary.Application.Identity.Authentication.Commands.TelegramVerify;
using FoodDiary.Application.Identity.Authentication.Commands.VerifyEmail;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.SetPassword;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class SecretInputLimitValidatorTests {
    private static readonly string OversizedPassword =
        new('p', AuthenticationInputLimits.MaximumPasswordLength + 1);

    private static readonly string OversizedToken =
        new('t', AuthenticationInputLimits.MaximumOpaqueTokenLength + 1);

    [Fact]
    public void IdentityPasswordValidators_RejectOversizedPasswords() {
        new LoginCommandValidator()
            .TestValidate(new LoginCommand("user@example.com", OversizedPassword))
            .ShouldHaveValidationErrorFor(command => command.Password);
        new RegisterCommandValidator()
            .TestValidate(new RegisterCommand("user@example.com", OversizedPassword, "en"))
            .ShouldHaveValidationErrorFor(command => command.Password);
        new RestoreAccountCommandValidator()
            .TestValidate(new RestoreAccountCommand("user@example.com", OversizedPassword))
            .ShouldHaveValidationErrorFor(command => command.Password);
    }

    [Fact]
    public void PasswordMutationValidators_RejectOversizedPasswords() {
        new ChangePasswordCommandValidator()
            .TestValidate(new ChangePasswordCommand(Guid.NewGuid(), OversizedPassword, OversizedPassword + "x"))
            .ShouldHaveValidationErrorFor(command => command.CurrentPassword);
        new SetPasswordCommandValidator()
            .TestValidate(new SetPasswordCommand(Guid.NewGuid(), OversizedPassword))
            .ShouldHaveValidationErrorFor(command => command.NewPassword);
        new SetAdminUserPasswordCommandValidator()
            .TestValidate(new SetAdminUserPasswordCommand(Guid.NewGuid(), Guid.NewGuid(), OversizedPassword))
            .ShouldHaveValidationErrorFor(command => command.NewPassword);
    }

    [Fact]
    public void CreateAdminUserValidator_RejectsOversizedTemporaryPassword() {
        var command = new CreateAdminUserCommand(
            "admin@example.com",
            FirstName: null,
            LastName: null,
            Language: "en",
            Roles: [RoleNames.Admin],
            TemporaryPassword: OversizedPassword,
            GeneratePassword: false,
            IsEmailConfirmed: true,
            SendCredentialsEmail: false,
            RequirePasswordChange: true,
            ClientOrigin: null,
            ActorUserId: Guid.NewGuid());

        new CreateAdminUserCommandValidator()
            .TestValidate(command)
            .ShouldHaveValidationErrorFor(value => value.TemporaryPassword);
    }

    [Fact]
    public void PasswordResetValidator_RejectsOversizedPasswordAndToken() {
        new ConfirmPasswordResetCommandValidator()
            .TestValidate(new ConfirmPasswordResetCommand(Guid.NewGuid(), OversizedToken, OversizedPassword))
            .ShouldHaveValidationErrorFor(command => command.Token);
        new ConfirmPasswordResetCommandValidator()
            .TestValidate(new ConfirmPasswordResetCommand(Guid.NewGuid(), OversizedToken, OversizedPassword))
            .ShouldHaveValidationErrorFor(command => command.NewPassword);
    }

    [Fact]
    public void OpaqueTokenValidators_RejectOversizedTokens() {
        new VerifyEmailCommandValidator()
            .TestValidate(new VerifyEmailCommand(Guid.NewGuid(), OversizedToken))
            .ShouldHaveValidationErrorFor(command => command.Token);
        new AcceptInvitationCommandValidator()
            .TestValidate(new AcceptInvitationCommand(Guid.NewGuid(), OversizedToken, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(command => command.Token);
        new DeclineInvitationCommandValidator()
            .TestValidate(new DeclineInvitationCommand(Guid.NewGuid(), OversizedToken, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(command => command.Token);
    }

    [Fact]
    public void ExternalAuthenticationValidators_RejectOversizedAssertions() {
        new TelegramVerifyCommandValidator()
            .TestValidate(new TelegramVerifyCommand(
                new string('t', AuthenticationInputLimits.MaximumTelegramInitDataLength + 1)))
            .ShouldHaveValidationErrorFor(command => command.InitData);
        new LinkTelegramCommandValidator()
            .TestValidate(new LinkTelegramCommand(
                Guid.NewGuid(),
                new string('t', AuthenticationInputLimits.MaximumTelegramInitDataLength + 1)))
            .ShouldHaveValidationErrorFor(command => command.InitData);
        new GoogleLoginCommandValidator()
            .TestValidate(new GoogleLoginCommand(
                new string('g', AuthenticationInputLimits.MaximumGoogleCredentialLength + 1)))
            .ShouldHaveValidationErrorFor(command => command.Credential);
        new RefreshTokenCommandValidator()
            .TestValidate(new RefreshTokenCommand(OversizedToken))
            .ShouldHaveValidationErrorFor(command => command.RefreshToken);
        new AdminSsoExchangeCommandValidator()
            .TestValidate(new AdminSsoExchangeCommand(
                new string('s', AuthenticationInputLimits.MaximumAdminSsoCodeLength + 1)))
            .ShouldHaveValidationErrorFor(command => command.Code);
    }

    [Fact]
    public void TelegramLoginWidgetValidator_RejectsOversizedTextFields() {
        var command = new TelegramLoginWidgetCommand(
            Id: 1,
            AuthDate: 1,
            Hash: new string('h', AuthenticationInputLimits.MaximumTelegramHashLength + 1),
            Username: new string('u', AuthenticationInputLimits.MaximumTelegramUsernameLength + 1),
            FirstName: new string('f', AuthenticationInputLimits.MaximumTelegramNameLength + 1),
            LastName: new string('l', AuthenticationInputLimits.MaximumTelegramNameLength + 1),
            PhotoUrl: new string('p', AuthenticationInputLimits.MaximumTelegramPhotoUrlLength + 1));
        TestValidationResult<TelegramLoginWidgetCommand> result =
            new TelegramLoginWidgetCommandValidator().TestValidate(command);

        Assert.Multiple(
            () => result.ShouldHaveValidationErrorFor(value => value.Hash),
            () => result.ShouldHaveValidationErrorFor(value => value.Username),
            () => result.ShouldHaveValidationErrorFor(value => value.FirstName),
            () => result.ShouldHaveValidationErrorFor(value => value.LastName),
            () => result.ShouldHaveValidationErrorFor(value => value.PhotoUrl));
    }

    [Fact]
    public void SensitiveCycleExportValidator_RejectsOversizedCurrentPassword() {
        var date = new DateOnly(2026, 8, 19);
        var query = new ExportCycleQuery(
            Guid.NewGuid(),
            date,
            date,
            Scope: CycleExportScope.Sensitive,
            CurrentPassword: OversizedPassword);

        new ExportCycleQueryValidator()
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(value => value.CurrentPassword);
    }
}
