using FluentValidation;
using DesiredWaistValueObject = FoodDiary.Domain.ValueObjects.DesiredWaist;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public class UpdateDesiredWaistCommandValidator : AbstractValidator<UpdateDesiredWaistCommand>
{
    public UpdateDesiredWaistCommandValidator()
    {
        RuleFor(c => c.DesiredWaist)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWaistValueObject.MaxValue)
            .When(c => c.DesiredWaist.HasValue);
    }
}
