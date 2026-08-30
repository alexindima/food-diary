using FluentValidation;
using FoodDiary.Application.Notifications.Common;

namespace FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;

public sealed class UpsertWebPushSubscriptionCommandValidator : AbstractValidator<UpsertWebPushSubscriptionCommand> {
    public UpsertWebPushSubscriptionCommandValidator() {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.Endpoint)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(WebPushEndpointPolicy.IsAllowed)
            .WithMessage("Endpoint must be an absolute HTTPS web push URL.");

        RuleFor(command => command.P256Dh)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(command => command.Auth)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(command => command.ExpirationTimeUtc)
            .Must(value => value is null || value.Value.Kind != DateTimeKind.Unspecified)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ExpirationTimeUtc timestamp kind must be specified.");

        RuleFor(command => command.Locale)
            .MaximumLength(16)
            .When(command => command.Locale is not null);

        RuleFor(command => command.UserAgent)
            .MaximumLength(512)
            .When(command => command.UserAgent is not null);
    }
}
