namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record TelegramStarsUpdateResultModel(bool CheckoutApproved, string? Error = null);
