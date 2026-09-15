using FluentValidation;

namespace FoodDiary.Modules.Hydration.Application.Queries.GetHydrationEntries;

public sealed class GetHydrationEntriesQueryValidator : AbstractValidator<GetHydrationEntriesQuery> {
    public GetHydrationEntriesQueryValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");
    }
}
