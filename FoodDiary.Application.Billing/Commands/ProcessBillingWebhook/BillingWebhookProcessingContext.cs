using FoodDiary.Domain.Entities.Billing;
using User = FoodDiary.Domain.Entities.Users.User;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed record BillingWebhookProcessingContext(BillingSubscription? Subscription, User User);
