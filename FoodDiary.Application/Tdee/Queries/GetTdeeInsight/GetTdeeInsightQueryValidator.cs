using FluentValidation;

namespace FoodDiary.Application.Tdee.Queries.GetTdeeInsight;

public class GetTdeeInsightQueryValidator : AbstractValidator<GetTdeeInsightQuery> {
    public GetTdeeInsightQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");
    }
}
