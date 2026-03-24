using FluentValidation;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public class GetDailyAdviceQueryValidator : AbstractValidator<GetDailyAdviceQuery> {
    public GetDailyAdviceQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
