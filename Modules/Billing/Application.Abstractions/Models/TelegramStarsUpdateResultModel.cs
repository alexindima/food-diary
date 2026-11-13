namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public union TelegramStarsUpdateResultModel(
    TelegramStarsUpdateResultModel.Approved,
    TelegramStarsUpdateResultModel.Processed,
    TelegramStarsUpdateResultModel.Rejected) {
    public static TelegramStarsUpdateResultModel CheckoutAccepted { get; } = new Approved();
    public static TelegramStarsUpdateResultModel UpdateProcessed { get; } = new Processed();

    public bool CheckoutApproved => this is Approved;
    public string? Error => this is Rejected rejected ? rejected.Reason : null;

    public sealed record Approved;
    public sealed record Processed;
    public sealed record Rejected(string Reason);
}
