using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;
using DesiredWaistValueObject = FoodDiary.Domain.ValueObjects.DesiredWaist;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public class UpdateDesiredWaistCommandValidator : AbstractValidator<UpdateDesiredWaistCommand> {
    public UpdateDesiredWaistCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.DesiredWaist)
            .GreaterThan(0)
            .LessThanOrEqualTo(DesiredWaistValueObject.MaxValue)
            .When(c => c.DesiredWaist.HasValue);
    }
}
