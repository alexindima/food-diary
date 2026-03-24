using FluentValidation;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public class DeleteWeightEntryCommandValidator : AbstractValidator<DeleteWeightEntryCommand> {
    public DeleteWeightEntryCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(c => c.WeightEntryId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("WeightEntryId is required.");
    }
}
