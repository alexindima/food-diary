namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface ITelegramAssertionReplayGuard {
    Task<bool> TryConsumeAsync(string signedAssertion, DateTime expiresAtUtc, DateTime authDateUtc, CancellationToken cancellationToken = default);
}
