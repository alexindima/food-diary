using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;
using DesiredWaistValueObject = FoodDiary.Domain.ValueObjects.DesiredWaist;
using DesiredWeightValueObject = FoodDiary.Domain.ValueObjects.DesiredWeight;

namespace FoodDiary.Application.Users.Commands.UpdateGoals;

public class UpdateGoalsCommandValidator : AbstractValidator<UpdateGoalsCommand> {
    public UpdateGoalsCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        When(x => x.DailyCalorieTarget.HasValue, () => {
            RuleFor(x => x.DailyCalorieTarget)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("DailyCalorieTarget must be greater than or equal to 0");
        });

        When(x => x.ProteinTarget.HasValue, () => {
            RuleFor(x => x.ProteinTarget)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("ProteinTarget must be greater than or equal to 0");
        });

        When(x => x.FatTarget.HasValue, () => {
            RuleFor(x => x.FatTarget)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("FatTarget must be greater than or equal to 0");
        });

        When(x => x.CarbTarget.HasValue, () => {
            RuleFor(x => x.CarbTarget)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("CarbTarget must be greater than or equal to 0");
        });

        When(x => x.FiberTarget.HasValue, () => {
            RuleFor(x => x.FiberTarget)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("FiberTarget must be greater than or equal to 0");
        });

        When(x => x.WaterGoal.HasValue, () => {
            RuleFor(x => x.WaterGoal)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("WaterGoal must be greater than or equal to 0");
        });

        When(x => x.DesiredWeight.HasValue, () => {
            RuleFor(x => x.DesiredWeight)
                .GreaterThan(0)
                .LessThanOrEqualTo(DesiredWeightValueObject.MaxValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage($"DesiredWeight must be in range (0, {DesiredWeightValueObject.MaxValue}]");
        });

        When(x => x.DesiredWaist.HasValue, () => {
            RuleFor(x => x.DesiredWaist)
                .GreaterThan(0)
                .LessThanOrEqualTo(DesiredWaistValueObject.MaxValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage($"DesiredWaist must be in range (0, {DesiredWaistValueObject.MaxValue}]");
        });
    }
}
