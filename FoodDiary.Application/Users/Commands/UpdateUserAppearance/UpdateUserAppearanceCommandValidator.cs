using FluentValidation;

namespace FoodDiary.Application.Users.Commands.UpdateUserAppearance;

public sealed class UpdateUserAppearanceCommandValidator : AbstractValidator<UpdateUserAppearanceCommand> {
    public UpdateUserAppearanceCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x)
            .Must(command => command.Theme is not null || command.UiStyle is not null)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("At least one appearance field must be provided.");
    }
}
