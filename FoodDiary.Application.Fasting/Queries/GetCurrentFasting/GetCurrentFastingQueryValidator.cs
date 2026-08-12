using FluentValidation;

namespace FoodDiary.Application.Fasting.Queries.GetCurrentFasting;

public sealed class GetCurrentFastingQueryValidator : AbstractValidator<GetCurrentFastingQuery> {
    public GetCurrentFastingQueryValidator() {
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
