using System;
using FluentValidation;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public class UpdateWeightEntryCommandValidator : AbstractValidator<UpdateWeightEntryCommand>
{
    public UpdateWeightEntryCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotNull()
            .WithMessage("UserId is required.");

        RuleFor(c => c.WeightEntryId)
            .NotNull();

        RuleFor(c => c.Weight)
            .GreaterThan(0)
            .LessThanOrEqualTo(500);

        RuleFor(c => c.Date)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("Date cannot be in the future.");
    }
}
