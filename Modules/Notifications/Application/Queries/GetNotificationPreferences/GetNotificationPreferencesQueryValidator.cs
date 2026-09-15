using FluentValidation;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryValidator : AbstractValidator<GetNotificationPreferencesQuery> {
    public GetNotificationPreferencesQueryValidator() {
        RuleFor(query => query.UserId)
            .NotEmpty();
    }
}
