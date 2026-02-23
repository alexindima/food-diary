using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public class GetHydrationEntriesQueryValidator : AbstractValidator<GetHydrationEntriesQuery> {
    public GetHydrationEntriesQueryValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");
    }
}
