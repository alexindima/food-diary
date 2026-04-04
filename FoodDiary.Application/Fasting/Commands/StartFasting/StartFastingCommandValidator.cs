using FluentValidation;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

public class StartFastingCommandValidator : AbstractValidator<StartFastingCommand> {
    public StartFastingCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.Protocol)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Fasting protocol is required");
    }
}
