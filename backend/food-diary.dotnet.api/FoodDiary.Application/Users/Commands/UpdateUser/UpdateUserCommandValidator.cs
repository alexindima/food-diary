using FluentValidation;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        When(x => x.Password != null, () =>
        {
            RuleFor(x => x.Password)
                .MinimumLength(6)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Password must be at least 6 characters");
        });

        When(x => x.Weight.HasValue, () =>
        {
            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Weight must be greater than 0");
        });

        When(x => x.Height.HasValue, () =>
        {
            RuleFor(x => x.Height)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Height must be greater than 0");
        });
    }
}
