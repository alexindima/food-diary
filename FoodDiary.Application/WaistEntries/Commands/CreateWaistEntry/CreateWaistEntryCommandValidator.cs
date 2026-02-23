using FluentValidation;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;

public class CreateWaistEntryCommandValidator : AbstractValidator<CreateWaistEntryCommand> {
    public CreateWaistEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.Circumference)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWaist.MaxValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Circumference must be in range (0, {DesiredWaist.MaxValue}].");
    }
}
