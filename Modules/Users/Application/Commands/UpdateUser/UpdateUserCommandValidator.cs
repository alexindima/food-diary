using FluentValidation;
using FoodDiary.Domain.ValueObjects;
using System.Globalization;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand> {
    public UpdateUserCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        When(x => x.WeightKg.HasValue, () => {
            RuleFor(x => x.WeightKg)
                .GreaterThan(0)
                .LessThanOrEqualTo(ProfileWeightKg.MaxValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(string.Create(CultureInfo.InvariantCulture, $"WeightKg must be in range (0, {ProfileWeightKg.MaxValue}]"));
        });

        When(x => x.HeightCm.HasValue, () => {
            RuleFor(x => x.HeightCm)
                .GreaterThan(0)
                .LessThanOrEqualTo(ProfileHeightCm.MaxValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(string.Create(CultureInfo.InvariantCulture, $"HeightCm must be in range (0, {ProfileHeightCm.MaxValue}]"));
        });

        When(x => x.StepGoal.HasValue, () => {
            RuleFor(x => x.StepGoal)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("StepGoal must be greater than or equal to 0");
        });
    }
}
