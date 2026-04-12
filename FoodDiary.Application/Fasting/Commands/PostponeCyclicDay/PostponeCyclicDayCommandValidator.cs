using FluentValidation;

namespace FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;

public sealed class PostponeCyclicDayCommandValidator : AbstractValidator<PostponeCyclicDayCommand> {
    public PostponeCyclicDayCommandValidator() {
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
