using FluentValidation;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;

public class GetWaistEntriesQueryValidator : AbstractValidator<GetWaistEntriesQuery> {
    public GetWaistEntriesQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .When(x => x.Limit.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Limit must be greater than zero.");

        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom.Value <= x.DateTo.Value)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("DateFrom must be earlier than or equal to DateTo.");
    }
}
