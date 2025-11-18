using FluentValidation;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public class DeleteWeightEntryCommandValidator : AbstractValidator<DeleteWeightEntryCommand>
{
    public DeleteWeightEntryCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotNull()
            .WithMessage("UserId is required.");

        RuleFor(c => c.WeightEntryId)
            .NotNull();
    }
}
