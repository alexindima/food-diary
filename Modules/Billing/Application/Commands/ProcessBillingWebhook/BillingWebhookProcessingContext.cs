using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

public sealed record BillingWebhookProcessingContext(BillingSubscription? Subscription, UserBillingProfileModel User, bool ShouldUpdateSubscription,
    BillingWebhookEventModel EffectiveEvent);
