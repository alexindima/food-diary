using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Billing {
        public static Error InvalidPlan => new(
            "Billing.InvalidPlan",
            "Billing plan is invalid.",
            Kind: ErrorKind.Validation);

        public static Error InvalidProvider(string provider) => new(
            "Billing.InvalidProvider",
            $"'{provider}' is not a valid billing provider.",
            Kind: ErrorKind.Validation);

        public static Error ProviderNotConfigured(string provider) => new(
            "Billing.ProviderNotConfigured",
            $"Billing provider '{provider}' is not configured.",
            Kind: ErrorKind.ExternalFailure);

        public static Error ProviderOperationFailed(string provider, string reason) => new(
            "Billing.ProviderOperationFailed",
            $"Billing provider '{provider}' request failed: {reason}",
            Kind: ErrorKind.ExternalFailure);

        public static Error SubscriptionAlreadyActive => new(
            "Billing.SubscriptionAlreadyActive",
            "Premium subscription is already active for the current user.",
            Kind: ErrorKind.Conflict);

        public static Error TrialAlreadyUsed => new(
            "Billing.TrialAlreadyUsed",
            "Premium trial has already been used for the current user.",
            Kind: ErrorKind.Conflict);

        public static Error CustomerPortalUnavailable => new(
            "Billing.CustomerPortalUnavailable",
            "Billing management is not available for the current user.",
            Kind: ErrorKind.NotFound);

        public static Error WebhookValidationFailed(string reason) => new(
            "Billing.WebhookValidationFailed",
            reason,
            Kind: ErrorKind.Validation);
    }
}
