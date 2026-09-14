using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;

public sealed record BillingWebhookProcessingContext(BillingSubscription? Subscription, UserBillingProfileModel User, bool ShouldUpdateSubscription);
