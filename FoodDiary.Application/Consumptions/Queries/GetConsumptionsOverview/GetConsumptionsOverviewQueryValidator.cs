using FluentValidation;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;

public sealed class GetConsumptionsOverviewQueryValidator : AbstractValidator<GetConsumptionsOverviewQuery> {
    public GetConsumptionsOverviewQueryValidator() {
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
