using FluentValidation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserLoginEvents;

public sealed class GetAdminUserLoginEventsQueryValidator : AbstractValidator<GetAdminUserLoginEventsQuery> {
    public GetAdminUserLoginEventsQueryValidator() {
        RuleFor(query => query.Provider).MaximumLength(100);
        RuleFor(query => query.Device).MaximumLength(100);
        RuleFor(query => query).Must(query => !query.FromUtc.HasValue || !query.ToUtc.HasValue || query.FromUtc < query.ToUtc)
            .WithMessage("The start must precede the exclusive end.");
    }
}
