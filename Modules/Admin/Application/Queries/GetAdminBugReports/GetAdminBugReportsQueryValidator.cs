using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminBugReports;

public sealed class GetAdminBugReportsQueryValidator : AbstractValidator<GetAdminBugReportsQuery> {
    public GetAdminBugReportsQueryValidator() {
        RuleFor(query => query.Filter.Page).InclusiveBetween(1, 10000);
        RuleFor(query => query.Filter.Limit).InclusiveBetween(1, 100);
        RuleFor(query => query.Filter.Search).MaximumLength(320);
        RuleFor(query => query.Filter.Status).MaximumLength(32);
        RuleFor(query => query.Filter).Must(filter => !filter.FromUtc.HasValue || !filter.ToUtc.HasValue || filter.FromUtc < filter.ToUtc)
            .WithMessage("The start must precede the exclusive end.");
    }
}
