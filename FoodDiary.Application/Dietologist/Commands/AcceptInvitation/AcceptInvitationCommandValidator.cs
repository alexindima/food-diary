using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitation;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand> {
    public AcceptInvitationCommandValidator() {
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
            .WithMessage("Token is required");
    }
}
