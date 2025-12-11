using FluentValidation;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public class DeleteHydrationEntryCommandValidator : AbstractValidator<DeleteHydrationEntryCommand>
{
    public DeleteHydrationEntryCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotNull();
    }
}
