namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserPasswordResetIssueModel(
    UserPasswordResetIssueStatus Status,
    UserPasswordResetDeliveryModel? Delivery = null);
