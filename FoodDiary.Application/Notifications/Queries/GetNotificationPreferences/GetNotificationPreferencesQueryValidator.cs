using FluentValidation;

namespace FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryValidator : AbstractValidator<GetNotificationPreferencesQuery> {
    public GetNotificationPreferencesQueryValidator() {
        RuleFor(query => query.UserId)
            .NotEmpty();
    }
}
