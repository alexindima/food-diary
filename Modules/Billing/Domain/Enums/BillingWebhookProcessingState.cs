namespace FoodDiary.Modules.Billing.Domain.Enums;

public enum BillingWebhookProcessingState {
    Unrecognized = 0,
    Received = 1,
    Failed = 2,
    Processed = 3,
}
