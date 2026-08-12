using FluentValidation;

namespace FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand> {
    public UpdateNotificationPreferencesCommandValidator() {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.FastingCheckInReminderHours)
            .InclusiveBetween(1, 168)
            .When(command => command.FastingCheckInReminderHours.HasValue);

        RuleFor(command => command.FastingCheckInFollowUpReminderHours)
            .InclusiveBetween(1, 168)
            .When(command => command.FastingCheckInFollowUpReminderHours.HasValue);

        RuleFor(command => command)
            .Must(command =>
                !command.FastingCheckInReminderHours.HasValue ||
                !command.FastingCheckInFollowUpReminderHours.HasValue ||
                command.FastingCheckInFollowUpReminderHours.Value > command.FastingCheckInReminderHours.Value)
            .WithMessage("Follow-up reminder hour must be greater than the first reminder hour.");
    }
}
