namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserPasswordResetIssueModel(
    UserPasswordResetIssueStatus Status,
    UserPasswordResetDeliveryModel? Delivery = null);
