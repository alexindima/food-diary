using FluentValidation;

namespace FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;

public class GetLatestWaistEntryQueryValidator : AbstractValidator<GetLatestWaistEntryQuery> {
    public GetLatestWaistEntryQueryValidator() {
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
