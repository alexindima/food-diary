using FluentValidation;

namespace FoodDiary.Modules.Meals.Application.Queries.GetMealsOverview;

public sealed class GetMealsOverviewQueryValidator : AbstractValidator<GetMealsOverviewQuery> {
    public GetMealsOverviewQueryValidator() {
        RuleFor(x => x.TimeZoneId)
            .Must((query, value) => FoodDiary.Application.Abstractions.Common.Validation.LocalCalendar.TryResolve(query.TimeZoneId, query.TimeZoneOffsetMinutes, out _))
            .WithMessage("Invalid time zone.");
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
