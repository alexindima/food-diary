using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

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
    }
}
