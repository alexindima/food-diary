using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public class CreateWeightEntryCommandValidator : AbstractValidator<CreateWeightEntryCommand> {
    public CreateWeightEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.Weight)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWeight.MaxValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Weight must be in range (0, {DesiredWeight.MaxValue}].");
    }
}
