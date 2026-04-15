using FluentValidation;

namespace FoodDiary.Application.Users.Queries.GetProfileOverview;

public sealed class GetProfileOverviewQueryValidator : AbstractValidator<GetProfileOverviewQuery> {
    public GetProfileOverviewQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
