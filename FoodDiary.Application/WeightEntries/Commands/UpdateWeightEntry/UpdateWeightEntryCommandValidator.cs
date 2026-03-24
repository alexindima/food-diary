using FluentValidation;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public class UpdateWeightEntryCommandValidator : AbstractValidator<UpdateWeightEntryCommand> {
    public UpdateWeightEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.WeightEntryId)
            .NotEqual(WeightEntryId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("WeightEntryId is required.");

        RuleFor(c => c.Weight)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWeight.MaxValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Weight must be in range (0, {DesiredWeight.MaxValue}].");
    }
}
