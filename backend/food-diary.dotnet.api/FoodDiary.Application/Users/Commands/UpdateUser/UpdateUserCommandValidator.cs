using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand> {
    public UpdateUserCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        When(x => x.Weight.HasValue, () => {
            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Weight must be greater than 0");
        });

        When(x => x.Height.HasValue, () => {
            RuleFor(x => x.Height)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Height must be greater than 0");
        });
    }
}
