using FluentValidation;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public sealed class GetFastingStatsQueryValidator : AbstractValidator<GetFastingStatsQuery> {
    public GetFastingStatsQueryValidator() {
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
