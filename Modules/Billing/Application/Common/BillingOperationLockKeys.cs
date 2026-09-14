namespace FoodDiary.Modules.Billing.Application.Common;

internal static class BillingOperationLockKeys {
    public static string ForUser(Guid userId) => $"billing-user:{userId:N}";
}
