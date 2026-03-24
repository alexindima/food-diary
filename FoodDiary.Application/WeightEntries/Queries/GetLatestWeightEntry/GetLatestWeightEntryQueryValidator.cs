using FluentValidation;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public class GetLatestWeightEntryQueryValidator : AbstractValidator<GetLatestWeightEntryQuery> {
    public GetLatestWeightEntryQueryValidator() {
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
