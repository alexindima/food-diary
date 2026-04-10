using FluentValidation;

namespace FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand> {
    public UpdateNotificationPreferencesCommandValidator() {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
