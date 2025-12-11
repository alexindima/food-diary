using FluentValidation;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

public class CreateHydrationEntryCommandValidator : AbstractValidator<CreateHydrationEntryCommand>
{
    public CreateHydrationEntryCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotNull();

        RuleFor(c => c.AmountMl)
            .GreaterThan(0)
            .LessThanOrEqualTo(10000);
    }
}
