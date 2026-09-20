using FluentValidation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Queries.GetDashboardSnapshot;

public sealed class GetDashboardSnapshotQueryValidator : AbstractValidator<GetDashboardSnapshotQuery> {
    public GetDashboardSnapshotQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.TimeZoneId)
            .Must((query, value) => FoodDiary.Application.Abstractions.Common.Validation.LocalCalendar.TryResolve(query.TimeZoneId, query.TimeZoneOffsetMinutes, out _))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Unknown time zone.");

        RuleFor(x => x.TimeZoneOffsetMinutes)
            .InclusiveBetween(-840, 840)
            .When(x => x.TimeZoneOffsetMinutes.HasValue);
    }
}
