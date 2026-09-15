using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Dietologist.Application.Commands.AcceptInvitation;
using FoodDiary.Modules.Dietologist.Application.Commands.DeclineInvitation;

namespace FoodDiary.Modules.Dietologist.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class SecretInputLimitValidatorTests {
    private static readonly string OversizedToken =
        new('t', AuthenticationInputLimits.MaximumOpaqueTokenLength + 1);

    [Fact]
    public void InvitationValidators_RejectOversizedTokens() {
        new AcceptInvitationCommandValidator()
            .TestValidate(new AcceptInvitationCommand(Guid.NewGuid(), OversizedToken, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(command => command.Token);
        new DeclineInvitationCommandValidator()
            .TestValidate(new DeclineInvitationCommand(Guid.NewGuid(), OversizedToken, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(command => command.Token);
    }
}
