using FluentValidation;
using FoodDiary.Application.Abstractions.Wearables.Common;

namespace FoodDiary.Application.Wearables.Wearables.Queries.GetWearableAuthUrl;

public sealed class GetWearableAuthUrlQueryValidator : AbstractValidator<GetWearableAuthUrlQuery> {
    public GetWearableAuthUrlQueryValidator() {
        RuleFor(query => query.UserId)
            .NotEmpty();
        RuleFor(query => query.Provider)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumProviderLength);
        RuleFor(query => query.State)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumOAuthStateLength);
    }
}
