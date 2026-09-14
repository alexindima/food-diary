namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

public sealed record PaddleNotificationRecoveryResult(int Inspected, int Replayed, bool WasLimited = false);
