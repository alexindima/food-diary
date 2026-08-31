using FluentValidation;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryValidator : AbstractValidator<GetUserAiUsageSummaryQuery> {
    public GetUserAiUsageSummaryQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");
    }
}
