using FluentValidation;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public class UpdateHydrationEntryCommandValidator : AbstractValidator<UpdateHydrationEntryCommand>
{
    public UpdateHydrationEntryCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotNull();

        RuleFor(c => c.AmountMl)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000)
            .When(c => c.AmountMl.HasValue);
    }
}
