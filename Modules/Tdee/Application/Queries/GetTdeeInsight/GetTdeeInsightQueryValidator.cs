using FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;
using FluentValidation;

namespace FoodDiary.Modules.Tdee.Application.Queries.GetTdeeInsight;

public sealed class GetTdeeInsightQueryValidator : AbstractValidator<GetTdeeInsightQuery> {
    public GetTdeeInsightQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");
    }
}
