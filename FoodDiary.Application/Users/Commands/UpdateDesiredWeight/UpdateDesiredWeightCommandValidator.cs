using FluentValidation;
using DesiredWeightValueObject = FoodDiary.Domain.ValueObjects.DesiredWeight;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public class UpdateDesiredWeightCommandValidator : AbstractValidator<UpdateDesiredWeightCommand> {
    public UpdateDesiredWeightCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.DesiredWeight)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"DesiredWeight must be in range (0, {DesiredWeightValueObject.MaxValue}]")
            .LessThanOrEqualTo(DesiredWeightValueObject.MaxValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"DesiredWeight must be in range (0, {DesiredWeightValueObject.MaxValue}]")
            .When(c => c.DesiredWeight.HasValue);
    }
}
