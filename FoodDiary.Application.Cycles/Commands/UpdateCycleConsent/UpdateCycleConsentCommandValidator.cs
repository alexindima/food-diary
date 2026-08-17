using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Commands.UpdateCycleConsent;

public sealed class UpdateCycleConsentCommandValidator : AbstractValidator<UpdateCycleConsentCommand> {
    public UpdateCycleConsentCommandValidator() {
        RuleFor(command => command.UserId)
            .NotNull()
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(command => command.CycleProfileId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("CycleProfileId is required.");

        RuleFor(command => command.Purpose)
            .Must(static purpose => Enum.IsDefined((CycleConsentPurpose)purpose))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Purpose is invalid.")
            .NotEqual((int)CycleConsentPurpose.CycleTracking)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Cycle tracking consent can only be withdrawn by deleting the cycle profile.");
    }
}
