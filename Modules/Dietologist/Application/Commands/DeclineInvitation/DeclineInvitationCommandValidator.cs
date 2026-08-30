using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitation;

public sealed class DeclineInvitationCommandValidator : AbstractValidator<DeclineInvitationCommand> {
    public DeclineInvitationCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.InvitationId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Invitation ID is required");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Token is required")
            .MaximumLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Token must not exceed {AuthenticationInputLimits.MaximumOpaqueTokenLength} characters");
    }
}
