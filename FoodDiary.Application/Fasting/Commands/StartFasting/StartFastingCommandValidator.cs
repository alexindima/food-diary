using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

public class StartFastingCommandValidator : AbstractValidator<StartFastingCommand> {
    public StartFastingCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.Protocol)
            .NotEmpty()
            .When(x => !string.Equals(x.PlanType, "Cyclic", StringComparison.OrdinalIgnoreCase))
            .WithErrorCode("Validation.Required")
            .WithMessage("Fasting protocol is required");

        RuleFor(x => x.PlanType)
            .Must(planType => string.IsNullOrWhiteSpace(planType) ||
                Enum.TryParse<FastingPlanType>(planType, ignoreCase: true, out _))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Fasting plan type is invalid.");
    }
}
