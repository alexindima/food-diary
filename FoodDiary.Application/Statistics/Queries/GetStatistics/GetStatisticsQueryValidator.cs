using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public class GetStatisticsQueryValidator : AbstractValidator<GetStatisticsQuery> {
    public GetStatisticsQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
