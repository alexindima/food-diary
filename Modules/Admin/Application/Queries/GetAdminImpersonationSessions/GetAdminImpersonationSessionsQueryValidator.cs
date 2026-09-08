using FluentValidation;

namespace FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;

public sealed class GetAdminImpersonationSessionsQueryValidator : AbstractValidator<GetAdminImpersonationSessionsQuery> {
    public GetAdminImpersonationSessionsQueryValidator() {
        RuleFor(query => query).Must(query => !query.FromUtc.HasValue || !query.ToUtc.HasValue || query.FromUtc < query.ToUtc)
            .WithMessage("The start must precede the exclusive end.");
    }
}
