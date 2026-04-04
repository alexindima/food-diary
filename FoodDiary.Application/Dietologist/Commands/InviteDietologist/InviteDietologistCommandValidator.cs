using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.InviteDietologist;

public class InviteDietologistCommandValidator : AbstractValidator<InviteDietologistCommand> {
    public InviteDietologistCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.DietologistEmail)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Dietologist email is required")
            .EmailAddress()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid email address");

        RuleFor(x => x.Permissions)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("Permissions are required");
    }
}
