namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public enum TelegramMessageDeliveryResult {
    Retry = 0,
    Sent = 1,
    RecipientUnavailable = 2,
}
