using FluentValidation;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public class UpdateDesiredWaistCommandValidator : AbstractValidator<UpdateDesiredWaistCommand>
{
    public UpdateDesiredWaistCommandValidator()
    {
        RuleFor(c => c.DesiredWaist)
            .GreaterThan(0)
            .LessThanOrEqualTo(300)
            .When(c => c.DesiredWaist.HasValue);
    }
}
