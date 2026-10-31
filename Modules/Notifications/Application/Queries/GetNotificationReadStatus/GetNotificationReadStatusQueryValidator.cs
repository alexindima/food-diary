using FluentValidation;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetNotificationReadStatus;

public sealed class GetNotificationReadStatusQueryValidator : AbstractValidator<GetNotificationReadStatusQuery> {
    public GetNotificationReadStatusQueryValidator() {
        RuleFor(query => query.NotificationIds).NotNull().Must(ids => ids is { Count: > 0 and <= 100 })
            .WithMessage("Provide between 1 and 100 notification ids.");
        RuleForEach(query => query.NotificationIds).NotEmpty();
    }
}
