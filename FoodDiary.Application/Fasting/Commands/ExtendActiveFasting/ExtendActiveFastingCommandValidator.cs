using FluentValidation;

namespace FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;

public class ExtendActiveFastingCommandValidator : AbstractValidator<ExtendActiveFastingCommand> {
    public ExtendActiveFastingCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.AdditionalHours)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Additional fasting hours must be greater than zero");
    }
}
