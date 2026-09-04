namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface ITelegramAssertionReplayGuard {
    Task<bool> TryConsumeAsync(string signedAssertion, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
}
