using FluentValidation;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public class GetDesiredWeightQueryValidator : AbstractValidator<GetDesiredWeightQuery> {
    public GetDesiredWeightQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
