using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvice;
using FluentValidation;

namespace FoodDiary.Modules.DailyAdvices.Application.Queries.GetDailyAdvice;

public sealed class GetDailyAdviceQueryValidator : AbstractValidator<GetDailyAdviceQuery> {
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
