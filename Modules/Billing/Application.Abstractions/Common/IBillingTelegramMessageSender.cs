using FoodDiary.Modules.Billing.Application.Abstractions.Models;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingTelegramMessageSender {
    Task<TelegramMessageDeliveryResult> SendAsync(long telegramUserId, string text, CancellationToken cancellationToken);
}
