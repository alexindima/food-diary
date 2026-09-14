namespace FoodDiary.Modules.Billing.Contracts.Models;

public sealed record PaddleNotificationRecoveryResult(int Inspected, int Replayed, bool WasLimited = false);
