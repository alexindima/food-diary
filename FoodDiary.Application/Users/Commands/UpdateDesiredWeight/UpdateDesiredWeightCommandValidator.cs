using FluentValidation;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public class UpdateDesiredWeightCommandValidator : AbstractValidator<UpdateDesiredWeightCommand>
{
    public UpdateDesiredWeightCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotNull();

        RuleFor(c => c.DesiredWeight)
            .GreaterThan(0)
            .LessThanOrEqualTo(500)
            .When(c => c.DesiredWeight.HasValue);
    }
}
