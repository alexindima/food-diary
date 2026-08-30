using System.Globalization;
using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Commands.CreateWeightEntry;

public sealed class CreateWeightEntryCommandValidator : AbstractValidator<CreateWeightEntryCommand> {
    public CreateWeightEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.WeightKg)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWeightKg.MaxValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage(string.Create(CultureInfo.InvariantCulture, $"WeightKg must be in range (0, {DesiredWeightKg.MaxValue}]."));
    }
}
