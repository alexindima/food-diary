using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;

public sealed class GetAdminAiUsageSummaryQueryValidator : AbstractValidator<GetAdminAiUsageSummaryQuery> {
    public GetAdminAiUsageSummaryQueryValidator() {
        RuleFor(x => x)
            .Must(q => !q.From.HasValue || !q.To.HasValue || q.From.Value <= q.To.Value)
            .WithMessage("'From' date must be less than or equal to 'To' date.");
    }
}
