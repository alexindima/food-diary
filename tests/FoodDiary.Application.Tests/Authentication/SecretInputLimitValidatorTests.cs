using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Admin.Commands.CreateAdminUser;
using FoodDiary.Application.Admin.Commands.SetAdminUserPassword;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.DeclineInvitation;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportCycle;
using FoodDiary.Application.Identity.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Identity.Authentication.Commands.Login;
using FoodDiary.Application.Identity.Authentication.Commands.Register;
using FoodDiary.Application.Identity.Authentication.Commands.RestoreAccount;
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
            .TestValidate(new SetAdminUserPasswordCommand(Guid.NewGuid(), OversizedPassword))
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
