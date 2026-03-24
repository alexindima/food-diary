using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public class DeleteWaistEntryCommandValidator : AbstractValidator<DeleteWaistEntryCommand> {
    public DeleteWaistEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.WaistEntryId)
            .NotEqual(WaistEntryId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("WaistEntryId is required.");
    }
}
