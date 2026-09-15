using FluentValidation;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;

namespace FoodDiary.Modules.Wearables.Application.Queries.GetWearableAuthUrl;

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
