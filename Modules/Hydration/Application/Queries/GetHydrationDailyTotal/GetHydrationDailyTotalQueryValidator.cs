using FluentValidation;

namespace FoodDiary.Modules.Hydration.Application.Queries.GetHydrationDailyTotal;

public sealed class GetHydrationDailyTotalQueryValidator : AbstractValidator<GetHydrationDailyTotalQuery> {
    public GetHydrationDailyTotalQueryValidator() {
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
