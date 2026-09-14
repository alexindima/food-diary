using FluentValidation;

namespace FoodDiary.Modules.Ai.Application.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryValidator : AbstractValidator<GetUserAiUsageSummaryQuery> {
    public GetUserAiUsageSummaryQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");
    }
}
