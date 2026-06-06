using System.Globalization;
using FluentValidation;
using DesiredWaistValueObject = FoodDiary.Domain.ValueObjects.DesiredWaist;
using DesiredWeightValueObject = FoodDiary.Domain.ValueObjects.DesiredWeight;

namespace FoodDiary.Application.Users.Commands.UpdateGoals;

public class UpdateGoalsCommandValidator : AbstractValidator<UpdateGoalsCommand> {
    public UpdateGoalsCommandValidator() {
        ConfigureUserRules();
        ConfigureMacroTargets();
        ConfigureBodyTargets();
        ConfigureDailyCalories();
    }

    private void ConfigureUserRules() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }

    private void ConfigureMacroTargets() {
        When(x => x.DailyCalorieTarget.HasValue, () => {
            RuleFor(x => x.DailyCalorieTarget)
                .Must(BeFinite)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("DailyCalorieTarget must be a finite number")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("DailyCalorieTarget must be greater than or equal to 0");
        });

        When(x => x.ProteinTarget.HasValue, () => {
            RuleFor(x => x.ProteinTarget)
                .Must(BeFinite)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("ProteinTarget must be a finite number")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("ProteinTarget must be greater than or equal to 0");
        });

        When(x => x.FatTarget.HasValue, () => {
            RuleFor(x => x.FatTarget)
                .Must(BeFinite)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("FatTarget must be a finite number")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("FatTarget must be greater than or equal to 0");
        });

        When(x => x.CarbTarget.HasValue, () => {
            RuleFor(x => x.CarbTarget)
                .Must(BeFinite)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("CarbTarget must be a finite number")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("CarbTarget must be greater than or equal to 0");
        });

        When(x => x.FiberTarget.HasValue, () => {
            RuleFor(x => x.FiberTarget)
                .Must(BeFinite)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("FiberTarget must be a finite number")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("FiberTarget must be greater than or equal to 0");
        });

        When(x => x.WaterGoal.HasValue, () => {
            RuleFor(x => x.WaterGoal)
                .Must(BeFinite)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("WaterGoal must be a finite number")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("WaterGoal must be greater than or equal to 0");
        });
    }

    private void ConfigureBodyTargets() {
        When(x => x.DesiredWeight.HasValue, () => {
            RuleFor(x => x.DesiredWeight)
                .GreaterThan(0)
                .LessThanOrEqualTo(DesiredWeightValueObject.MaxValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(string.Create(CultureInfo.InvariantCulture, $"DesiredWeight must be in range (0, {DesiredWeightValueObject.MaxValue}]"));
        });

        When(x => x.DesiredWaist.HasValue, () => {
            RuleFor(x => x.DesiredWaist)
                .GreaterThan(0)
                .LessThanOrEqualTo(DesiredWaistValueObject.MaxValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(string.Create(CultureInfo.InvariantCulture, $"DesiredWaist must be in range (0, {DesiredWaistValueObject.MaxValue}]"));
        });
    }

    private void ConfigureDailyCalories() {
        When(x => x.MondayCalories.HasValue, () => {
            RuleFor(x => x.MondayCalories).Must(BeFinite)
                .WithErrorCode("Validation.Invalid").WithMessage("MondayCalories must be finite")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid").WithMessage("MondayCalories must be >= 0");
        });
        When(x => x.TuesdayCalories.HasValue, () => {
            RuleFor(x => x.TuesdayCalories).Must(BeFinite)
                .WithErrorCode("Validation.Invalid").WithMessage("TuesdayCalories must be finite")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid").WithMessage("TuesdayCalories must be >= 0");
        });
        When(x => x.WednesdayCalories.HasValue, () => {
            RuleFor(x => x.WednesdayCalories).Must(BeFinite)
                .WithErrorCode("Validation.Invalid").WithMessage("WednesdayCalories must be finite")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid").WithMessage("WednesdayCalories must be >= 0");
        });
        When(x => x.ThursdayCalories.HasValue, () => {
            RuleFor(x => x.ThursdayCalories).Must(BeFinite)
                .WithErrorCode("Validation.Invalid").WithMessage("ThursdayCalories must be finite")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid").WithMessage("ThursdayCalories must be >= 0");
        });
        When(x => x.FridayCalories.HasValue, () => {
            RuleFor(x => x.FridayCalories).Must(BeFinite)
                .WithErrorCode("Validation.Invalid").WithMessage("FridayCalories must be finite")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid").WithMessage("FridayCalories must be >= 0");
        });
        When(x => x.SaturdayCalories.HasValue, () => {
            RuleFor(x => x.SaturdayCalories).Must(BeFinite)
                .WithErrorCode("Validation.Invalid").WithMessage("SaturdayCalories must be finite")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid").WithMessage("SaturdayCalories must be >= 0");
        });
        When(x => x.SundayCalories.HasValue, () => {
            RuleFor(x => x.SundayCalories).Must(BeFinite)
                .WithErrorCode("Validation.Invalid").WithMessage("SundayCalories must be finite")
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid").WithMessage("SundayCalories must be >= 0");
        });
    }

    private static bool BeFinite(double? value) => !value.HasValue || double.IsFinite(value.Value);
}
