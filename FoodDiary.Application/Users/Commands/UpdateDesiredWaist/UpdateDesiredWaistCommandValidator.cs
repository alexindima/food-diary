using FluentValidation;
using DesiredWaistValueObject = FoodDiary.Domain.ValueObjects.DesiredWaist;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public class UpdateDesiredWaistCommandValidator : AbstractValidator<UpdateDesiredWaistCommand> {
    public UpdateDesiredWaistCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.DesiredWaist)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"DesiredWaist must be in range (0, {DesiredWaistValueObject.MaxValue}]")
            .LessThanOrEqualTo(DesiredWaistValueObject.MaxValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"DesiredWaist must be in range (0, {DesiredWaistValueObject.MaxValue}]")
            .When(c => c.DesiredWaist.HasValue);
    }
}
