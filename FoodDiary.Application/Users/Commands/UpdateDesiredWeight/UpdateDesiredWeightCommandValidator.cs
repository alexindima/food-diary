using FluentValidation;
using DesiredWeightValueObject = FoodDiary.Domain.ValueObjects.DesiredWeight;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public class UpdateDesiredWeightCommandValidator : AbstractValidator<UpdateDesiredWeightCommand>
{
    public UpdateDesiredWeightCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotNull();

        RuleFor(c => c.DesiredWeight)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWeightValueObject.MaxValue)
            .When(c => c.DesiredWeight.HasValue);
    }
}
