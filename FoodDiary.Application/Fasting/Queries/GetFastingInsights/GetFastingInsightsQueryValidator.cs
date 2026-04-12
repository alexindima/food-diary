using FluentValidation;

namespace FoodDiary.Application.Fasting.Queries.GetFastingInsights;

public sealed class GetFastingInsightsQueryValidator : AbstractValidator<GetFastingInsightsQuery> {
    public GetFastingInsightsQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
