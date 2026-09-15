using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Admin.Application.Commands.CreateAdminUser;
using FoodDiary.Modules.Admin.Application.Commands.SetAdminUserPassword;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Admin.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class SecretInputLimitValidatorTests {
    private static readonly string OversizedPassword =
        new('p', AuthenticationInputLimits.MaximumPasswordLength + 1);

    [Fact]
    public void SetAdminPasswordValidator_RejectsOversizedPassword() {
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
}
