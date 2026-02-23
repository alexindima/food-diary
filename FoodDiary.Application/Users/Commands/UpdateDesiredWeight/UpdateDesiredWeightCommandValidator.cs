using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;
using DesiredWeightValueObject = FoodDiary.Domain.ValueObjects.DesiredWeight;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public class UpdateDesiredWeightCommandValidator : AbstractValidator<UpdateDesiredWeightCommand> {
    public UpdateDesiredWeightCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.DesiredWeight)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWeightValueObject.MaxValue)
            .When(c => c.DesiredWeight.HasValue);
    }
}
