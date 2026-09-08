using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryValidator : AbstractValidator<GetAdminContentReportsQuery> {
    public GetAdminContentReportsQueryValidator() {
        RuleFor(query => query.ToUtc).GreaterThan(query => query.FromUtc).When(query => query.FromUtc.HasValue && query.ToUtc.HasValue);
        RuleFor(query => query.TargetType).Must(value => value is null or "" or "Recipe" or "Comment");
        RuleFor(query => query.ReporterId).NotEqual(Guid.Empty);
        RuleFor(query => query.TargetId).NotEqual(Guid.Empty);
    }
}
