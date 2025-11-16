using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand> {
    public UpdateUserCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        When(x => x.Weight.HasValue, () => {
            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Weight must be greater than 0");
        });

        When(x => x.Height.HasValue, () => {
            RuleFor(x => x.Height)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Height must be greater than 0");
        });

        When(x => x.DailyCalorieTarget.HasValue, () => {
            RuleFor(x => x.DailyCalorieTarget)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("DailyCalorieTarget must be greater than 0");
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

        When(x => x.StepGoal.HasValue, () => {
            RuleFor(x => x.StepGoal)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("StepGoal must be greater than or equal to 0");
        });

        When(x => x.WaterGoal.HasValue, () => {
            RuleFor(x => x.WaterGoal)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("WaterGoal must be greater than or equal to 0");
        });
    }
}
