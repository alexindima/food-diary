using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Application.Commands.ChangePassword;
using FoodDiary.Modules.Users.Application.Commands.SetPassword;

namespace FoodDiary.Modules.Users.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class SecretInputLimitValidatorTests {
    private static readonly string OversizedPassword =
        new('p', AuthenticationInputLimits.MaximumPasswordLength + 1);

    [Fact]
    public void PasswordMutationValidators_RejectOversizedPasswords() {
        new ChangePasswordCommandValidator()
            .TestValidate(new ChangePasswordCommand(Guid.NewGuid(), OversizedPassword, OversizedPassword + "x"))
            .ShouldHaveValidationErrorFor(command => command.CurrentPassword);
        new SetPasswordCommandValidator()
            .TestValidate(new SetPasswordCommand(Guid.NewGuid(), OversizedPassword))
            .ShouldHaveValidationErrorFor(command => command.NewPassword);
    }
}
