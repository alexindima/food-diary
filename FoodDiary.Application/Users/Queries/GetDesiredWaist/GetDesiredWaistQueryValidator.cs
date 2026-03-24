using FluentValidation;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public class GetDesiredWaistQueryValidator : AbstractValidator<GetDesiredWaistQuery> {
    public GetDesiredWaistQueryValidator() {
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
