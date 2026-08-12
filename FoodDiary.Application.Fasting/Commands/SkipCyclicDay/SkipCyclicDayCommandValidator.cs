using FluentValidation;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicDay;

public sealed class SkipCyclicDayCommandValidator : AbstractValidator<SkipCyclicDayCommand> {
    public SkipCyclicDayCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
